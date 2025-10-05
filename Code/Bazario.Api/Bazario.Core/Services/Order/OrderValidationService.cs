using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.Models.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order validation and business rules
    /// Handles order validation, business rule checks, and calculations
    /// </summary>
    public class OrderValidationService : IOrderValidationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IShippingZoneService _shippingZoneService;
        private readonly IDiscountRepository _discountRepository;
        private readonly ILogger<OrderValidationService> _logger;

        public OrderValidationService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IShippingZoneService shippingZoneService,
            IDiscountRepository discountRepository,
            ILogger<OrderValidationService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _shippingZoneService = shippingZoneService ?? throw new ArgumentNullException(nameof(shippingZoneService));
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if order can be modified: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return false;
                }

                // Orders can only be modified if they are in Pending status
                var canModify = order.Status == OrderStatus.Pending.ToString();

                _logger.LogDebug("Order {OrderId} can be modified: {CanModify}", orderId, canModify);

                return canModify;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be modified: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CanOrderBeCancelledAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if order can be cancelled: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return false;
                }

                // Orders can be cancelled if they are Pending or Processing
                var canCancel = order.Status == OrderStatus.Pending.ToString() ||
                               order.Status == OrderStatus.Processing.ToString();

                _logger.LogDebug("Order {OrderId} can be cancelled: {CanCancel}", orderId, canCancel);

                return canCancel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be cancelled: {OrderId}", orderId);
                throw;
            }
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            _logger.LogDebug("Validating status transition from {CurrentStatus} to {NewStatus}", currentStatus, newStatus);

            // Define valid status transitions
            var validTransitions = new Dictionary<string, List<string>>
            {
                { OrderStatus.Pending.ToString(), new List<string> { OrderStatus.Processing.ToString(), OrderStatus.Cancelled.ToString() } },
                { OrderStatus.Processing.ToString(), new List<string> { OrderStatus.Shipped.ToString(), OrderStatus.Cancelled.ToString() } },
                { OrderStatus.Shipped.ToString(), new List<string> { OrderStatus.Delivered.ToString() } },
                { OrderStatus.Delivered.ToString(), new List<string>() }, // Terminal state
                { OrderStatus.Cancelled.ToString(), new List<string>() }  // Terminal state
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                _logger.LogWarning("Unknown current status: {CurrentStatus}", currentStatus);
                return false;
            }

            var isValid = validTransitions[currentStatus].Contains(newStatus);

            _logger.LogDebug("Status transition from {CurrentStatus} to {NewStatus} is valid: {IsValid}",
                currentStatus, newStatus, isValid);

            return isValid;
        }

        public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(List<OrderItemAddRequest> orderItems, Guid customerId, ShippingAddress shippingAddress, List<string>? discountCodes = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Calculating order total for {ItemCount} items, customer: {CustomerId}, shipping to: {City}, {State}",
                orderItems?.Count ?? 0, customerId, shippingAddress?.City, shippingAddress?.State);

            try
            {
                if (orderItems == null || !orderItems.Any())
                {
                    throw new ArgumentException("Order items cannot be null or empty", nameof(orderItems));
                }

                // Validate shipping address
                if (shippingAddress == null)
                {
                    throw new ArgumentException("Shipping address is required for order calculation", nameof(shippingAddress));
                }

                if (string.IsNullOrWhiteSpace(shippingAddress.City))
                {
                    throw new ArgumentException("City is required for shipping calculation", nameof(shippingAddress.City));
                }

                decimal subtotal = 0;

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product {item.ProductId} not found");
                    }

                    subtotal += product.Price * item.Quantity;
                }

                // Get products and group by store for efficient processing
                var productStoreMap = new Dictionary<Guid, List<OrderItemAddRequest>>();
                var storeIds = new HashSet<Guid>();

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product != null)
                    {
                        var storeId = product.StoreId;
                        storeIds.Add(storeId);
                        
                        if (!productStoreMap.ContainsKey(storeId))
                        {
                            productStoreMap[storeId] = new List<OrderItemAddRequest>();
                        }
                        
                        productStoreMap[storeId].Add(item);
                    }
                }

                _logger.LogDebug("Found {StoreCount} unique stores in order", storeIds.Count);

                // Calculate shipping cost for each store using store-specific shipping configuration
                decimal totalShippingCost = 0;
                foreach (var storeId in storeIds)
                {
                    ShippingZone storeShippingZone;
                    decimal storeDeliveryFee;
                    
                    try
                    {
                        // Determine store-specific shipping zone
                        storeShippingZone = await _shippingZoneService.DetermineStoreShippingZoneAsync(
                            storeId,
                            shippingAddress.City, 
                            "EG", 
                            cancellationToken);

                        _logger.LogDebug("Store {StoreId} shipping zone: {ShippingZone} for address: {City}, {State}", 
                            storeId, storeShippingZone, shippingAddress.City, shippingAddress.State);

                        // Check if shipping is not supported for this address
                        if (storeShippingZone == ShippingZone.NotSupported)
                        {
                            throw new InvalidOperationException($"Shipping is not supported to the address: {shippingAddress.City}, {shippingAddress.State}. Only Egyptian addresses are currently supported.");
                        }

                        // Get store-specific delivery fee
                        storeDeliveryFee = await _shippingZoneService.GetStoreDeliveryFeeAsync(
                            storeId, 
                            shippingAddress.City, 
                            "EG", 
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to determine store-specific shipping for store {StoreId}, using fallback", storeId);
                        
                        // Fallback to simple zone determination
                        storeShippingZone = GetSimpleFallbackZone(shippingAddress.City, "EG");
                        storeDeliveryFee = GetSimpleDeliveryFee(storeShippingZone);
                        
                        _logger.LogDebug("Using fallback shipping zone: {ShippingZone}, fee: {Fee} for store {StoreId}", 
                            storeShippingZone, storeDeliveryFee, storeId);
                    }

                    // Calculate subtotal for this store
                    var storeSubtotal = productStoreMap[storeId]
                        .Sum(x => x.Price * x.Quantity);

                    // Use the store delivery fee from the new configuration system
                    decimal storeShippingCost = storeDeliveryFee;

                    totalShippingCost += storeShippingCost;

                    _logger.LogDebug("Store {StoreId} shipping cost: {ShippingCost} (subtotal: {Subtotal}, store fee: {StoreFee})", 
                        storeId, storeShippingCost, storeSubtotal, storeDeliveryFee);
                }

                _logger.LogDebug("Total shipping cost: {TotalShippingCost}", totalShippingCost);
                
                // Calculate discount amount for multiple discount codes
                decimal totalDiscountAmount = 0;
                var appliedDiscounts = new List<string>();
                var appliedDiscountTypes = new List<string>();
                
                if (discountCodes != null && discountCodes.Any())
                {
                    _logger.LogDebug("Validating {DiscountCount} discount codes: {DiscountCodes}", 
                        discountCodes.Count, string.Join(", ", discountCodes));
                    
                    foreach (var discountCode in discountCodes)
                    {
                        if (string.IsNullOrWhiteSpace(discountCode))
                            continue;
                            
                        _logger.LogDebug("Validating discount code: {DiscountCode}", discountCode);
                        
                        var (isValid, discount, errorMessage) = await _discountRepository.ValidateDiscountAsync(
                            discountCode, 
                            subtotal, 
                            storeIds.ToList(), 
                            cancellationToken);
                        
                        if (isValid && discount != null)
                        {
                            // Calculate discount amount based on type
                            decimal currentDiscountAmount = 0;
                            if (discount.Type == DiscountType.Percentage)
                            {
                                currentDiscountAmount = subtotal * discount.Value; // Value is already a decimal (0.10 = 10%)
                            }
                            else if (discount.Type == DiscountType.FixedAmount)
                            {
                                currentDiscountAmount = discount.Value; // Value is the fixed amount in EGP
                            }
                            
                            // Ensure discount doesn't exceed remaining subtotal
                            currentDiscountAmount = Math.Min(currentDiscountAmount, subtotal - totalDiscountAmount);
                            
                            if (currentDiscountAmount > 0)
                            {
                                totalDiscountAmount += currentDiscountAmount;
                                appliedDiscounts.Add($"{discount.Code} ({discount.Type})");
                                appliedDiscountTypes.Add(discount.Type.ToString());
                                
                                _logger.LogDebug("Applied discount: {DiscountCode}, Amount: {DiscountAmount}", 
                                    discountCode, currentDiscountAmount);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid discount code: {DiscountCode}, Error: {ErrorMessage}", 
                                discountCode, errorMessage);
                        }
                    }
                }
                
                // Calculate total (no tax for now)
                // Formula: total = subtotal + shippingCost - totalDiscountAmount
                var total = subtotal + totalShippingCost - totalDiscountAmount;

                var calculation = new OrderTotalCalculation
                {
                    Subtotal = subtotal,
                    TaxAmount = 0, // No tax for now
                    DiscountAmount = totalDiscountAmount,
                    ShippingCost = totalShippingCost,
                    Total = total,
                    AppliedDiscounts = appliedDiscounts
                };

                _logger.LogDebug("Order total calculated: Subtotal: {Subtotal}, Shipping: {ShippingCost}, Discount: {DiscountAmount}, Total: {Total}",
                    subtotal, totalShippingCost, totalDiscountAmount, total);

                return calculation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate order total");
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailabilityAsync(List<OrderItemAddRequest> orderItems, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating stock availability for {ItemCount} items", orderItems?.Count ?? 0);

            try
            {
                if (orderItems == null || !orderItems.Any())
                {
                    _logger.LogWarning("Order validation failed: No order items provided");
                    return false;
                }

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        _logger.LogWarning("Product not found: {ProductId}", item.ProductId);
                        return false;
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product: {ProductId}. Required: {Required}, Available: {Available}",
                            item.ProductId, item.Quantity, product.StockQuantity);
                        return false;
                    }
                }

                _logger.LogDebug("Stock availability validation passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate stock availability");
                throw;
            }
        }

        /// <summary>
        /// Simple fallback zone determination for when store-specific methods fail
        /// </summary>
        private ShippingZone GetSimpleFallbackZone(string city, string country)
        {
            if (string.IsNullOrWhiteSpace(country) || country.ToUpperInvariant() != "EG")
            {
                return ShippingZone.NotSupported;
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return ShippingZone.Local;
            }

            var cityUpper = city.ToUpperInvariant();
            
            // Major cities (local delivery)
            if (cityUpper == "CAIRO")
            {
                return ShippingZone.Local;
            }
            
            // Major cities (national delivery)
            if (cityUpper == "ALEXANDRIA" || cityUpper == "GIZA" || cityUpper == "PORT SAID" || cityUpper == "SUEZ" || 
                cityUpper == "LUXOR" || cityUpper == "ASWAN" || cityUpper == "HURGHADA")
            {
                return ShippingZone.National;
            }
            
            // Default to local for other Egyptian cities
            return ShippingZone.Local;
        }

        /// <summary>
        /// Simple fallback delivery fee calculation based on shipping zone
        /// </summary>
        private decimal GetSimpleDeliveryFee(ShippingZone zone)
        {
            return zone switch
            {
                ShippingZone.SameDay => 0m,     // Same-day delivery fee (store must configure)
                ShippingZone.Local => 0m,       // Local delivery fee (store must configure)
                ShippingZone.National => 0m,    // National delivery fee (store must configure)
                ShippingZone.NotSupported => 0m, // Not supported - no fee
                _ => 0m                         // Default fallback (store must configure)
            };
        }
    }
}
