using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.Helpers.Order;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Enums.Inventory;

namespace Bazario.Core.Services.Order
{
    public class OrderValidationService : IOrderValidationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly OrderCalculator _orderCalculator;
        private readonly ILogger<OrderValidationService> _logger;

        public OrderValidationService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            OrderCalculator orderCalculator,
            ILogger<OrderValidationService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderCalculator = orderCalculator ?? throw new ArgumentNullException(nameof(orderCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

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
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be modified: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to check if order {orderId} can be modified: {ex.Message}", ex);
            }
        }

        public async Task<bool> CanOrderBeCancelledAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

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
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be cancelled: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to check if order {orderId} can be cancelled: {ex.Message}", ex);
            }
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(currentStatus))
            {
                throw new ArgumentException("Current status cannot be null or empty", nameof(currentStatus));
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                throw new ArgumentException("New status cannot be null or empty", nameof(newStatus));
            }

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

        public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(
            List<OrderItemAddRequest> orderItems,
            Guid customerId,
            ShippingAddress shippingAddress,
            List<string>? discountCodes = null,
            CancellationToken cancellationToken = default)
        {
            // Note: customerId parameter is currently unused but reserved for future customer-specific pricing/discounts
            // Validate for consistency even though not used in current implementation
            if (customerId == Guid.Empty)
            {
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
            }

            _logger.LogDebug("Calculating order total for {ItemCount} items, customer: {CustomerId}, shipping to: {City}, {State}",
                orderItems?.Count ?? 0, customerId, shippingAddress?.City, shippingAddress?.State);

            try
            {
                // Add null checks before validation
                if (orderItems == null)
                {
                    throw new ArgumentNullException(nameof(orderItems), "Order items cannot be null");
                }

                if (shippingAddress == null)
                {
                    throw new ArgumentNullException(nameof(shippingAddress), "Shipping address cannot be null");
                }

                // Validate inputs
                ValidateCalculationInputs(orderItems, shippingAddress);

                // Step 1: Calculate subtotal
                var subtotal = await _orderCalculator.CalculateSubtotalAsync(orderItems, cancellationToken);

                // Step 2: Group items by store
                var itemsByStore = await _orderCalculator.GroupItemsByStoreAsync(orderItems, cancellationToken);
                var storeIds = itemsByStore.Keys.ToList();

                // Step 3: Calculate shipping cost
                var shippingCost = await _orderCalculator.CalculateShippingCostAsync(
                    itemsByStore,
                    shippingAddress,
                    cancellationToken);

                // Step 4: Calculate discounts
                var (discountAmount, appliedDiscounts, appliedDiscountTypes) = await _orderCalculator.CalculateDiscountsAsync(
                    discountCodes,
                    subtotal,
                    storeIds,
                    cancellationToken);

                // Step 5: Calculate total
                var total = subtotal + shippingCost - discountAmount;

                var calculation = new OrderTotalCalculation
                {
                    Subtotal = subtotal,
                    TaxAmount = 0, // No tax for now
                    DiscountAmount = discountAmount,
                    ShippingCost = shippingCost,
                    Total = total,
                    AppliedDiscounts = appliedDiscounts
                };

                _logger.LogDebug("Order total calculated: Subtotal: {Subtotal}, Shipping: {ShippingCost}, Discount: {DiscountAmount}, Total: {Total}",
                    subtotal, shippingCost, discountAmount, total);

                return calculation;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate order total");
                throw new InvalidOperationException($"Failed to calculate order total: {ex.Message}", ex);
            }
        }

        private void ValidateCalculationInputs(List<OrderItemAddRequest> orderItems, ShippingAddress shippingAddress)
        {
            // Note: Null checks are already performed by caller, this validates semantic requirements
            if (orderItems.Count == 0)
            {
                throw new ArgumentException("Order items cannot be empty", nameof(orderItems));
            }

            if (string.IsNullOrWhiteSpace(shippingAddress.City))
            {
                throw new ArgumentException("City is required for shipping calculation", nameof(shippingAddress.City));
            }
        }

        public void ValidateOrderUpdateBusinessRules(OrderUpdateRequest orderUpdateRequest, Domain.Entities.Order.Order existingOrder)
        {
            // Validate inputs
            if (orderUpdateRequest == null)
            {
                throw new ArgumentNullException(nameof(orderUpdateRequest), "Order update request cannot be null");
            }

            if (existingOrder == null)
            {
                throw new ArgumentNullException(nameof(existingOrder), "Existing order cannot be null");
            }

            _logger.LogDebug("Validating business rules for order update: {OrderId}", orderUpdateRequest.OrderId);

            // Get the values that will be used for validation (either from request or existing order)
            var subtotal = orderUpdateRequest.Subtotal ?? existingOrder.Subtotal;
            var discountAmount = orderUpdateRequest.DiscountAmount ?? existingOrder.DiscountAmount;
            var shippingCost = orderUpdateRequest.ShippingCost ?? existingOrder.ShippingCost;
            var totalAmount = orderUpdateRequest.TotalAmount ?? existingOrder.TotalAmount;

            // Business Rule 1: Discount amount should not exceed subtotal
            if (discountAmount > subtotal)
            {
                _logger.LogWarning("Business rule violation: Discount amount {DiscountAmount} exceeds subtotal {Subtotal} for order {OrderId}",
                    discountAmount, subtotal, orderUpdateRequest.OrderId);
                throw new InvalidOperationException($"Discount amount ({discountAmount:C}) cannot exceed subtotal ({subtotal:C})");
            }

            // Business Rule 2: Total amount should be consistent with subtotal, discount, and shipping
            var expectedTotal = subtotal + shippingCost - discountAmount;
            if (Math.Abs(totalAmount - expectedTotal) > 0.01m) // Allow for small rounding differences
            {
                _logger.LogWarning("Business rule violation: Total amount {TotalAmount} does not match calculated total {ExpectedTotal} (Subtotal: {Subtotal}, Shipping: {ShippingCost}, Discount: {DiscountAmount}) for order {OrderId}",
                    totalAmount, expectedTotal, subtotal, shippingCost, discountAmount, orderUpdateRequest.OrderId);
                throw new InvalidOperationException($"Total amount ({totalAmount:C}) does not match calculated total ({expectedTotal:C}). Expected: Subtotal + Shipping - Discount = {subtotal:C} + {shippingCost:C} - {discountAmount:C}");
            }

            // Business Rule 3: AppliedDiscountCodes and AppliedDiscountTypes should be consistent
            if (!string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountCodes) && !string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountTypes))
            {
                var codeCount = orderUpdateRequest.AppliedDiscountCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                var typeCount = orderUpdateRequest.AppliedDiscountTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

                if (codeCount != typeCount)
                {
                    _logger.LogWarning("Business rule violation: AppliedDiscountCodes count ({CodeCount}) does not match AppliedDiscountTypes count ({TypeCount}) for order {OrderId}",
                        codeCount, typeCount, orderUpdateRequest.OrderId);
                    throw new InvalidOperationException($"Applied discount codes count ({codeCount}) must match applied discount types count ({typeCount})");
                }
            }

            // Business Rule 4: If discount amount is provided, discount codes should also be provided
            if (orderUpdateRequest.DiscountAmount.HasValue && orderUpdateRequest.DiscountAmount.Value > 0 &&
                string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountCodes))
            {
                _logger.LogWarning("Business rule violation: Discount amount {DiscountAmount} provided but no discount codes for order {OrderId}",
                    orderUpdateRequest.DiscountAmount.Value, orderUpdateRequest.OrderId);
                throw new InvalidOperationException("Discount amount cannot be greater than 0 without applied discount codes");
            }

            _logger.LogDebug("Business rules validation passed for order update: {OrderId}", orderUpdateRequest.OrderId);
        }

        public async Task<StockValidationResult> ValidateStockAvailabilityWithDetailsAsync(
            List<OrderItemAddRequest> orderItems,
            CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderItems == null)
            {
                throw new ArgumentNullException(nameof(orderItems), "Order items cannot be null");
            }

            _logger.LogDebug("Validating stock availability with details for {ItemCount} items", orderItems.Count);

            var result = new StockValidationResult
            {
                IsValid = true,
                Message = "All items are in stock"
            };

            try
            {
                if (orderItems.Count == 0)
                {
                    _logger.LogWarning("Order validation failed: No order items provided");
                    result.IsValid = false;
                    result.Message = "No order items provided";
                    return result;
                }

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);

                    if (product == null)
                    {
                        _logger.LogWarning("Product not found: {ProductId}", item.ProductId);
                        result.IsValid = false;
                        result.InvalidItems.Add(new StockValidationItem
                        {
                            ProductId = item.ProductId,
                            ProductName = null,
                            RequestedQuantity = item.Quantity,
                            AvailableQuantity = null,
                            FailureReason = StockValidationFailureReason.ProductNotFound,
                            ErrorMessage = $"Product with ID {item.ProductId} not found"
                        });
                        continue;
                    }

                    // Check if product is inactive
                    if (product.IsDeleted)
                    {
                        _logger.LogWarning("Product is inactive: {ProductId} ({ProductName})", item.ProductId, product.Name);
                        result.IsValid = false;
                        result.InvalidItems.Add(new StockValidationItem
                        {
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            RequestedQuantity = item.Quantity,
                            AvailableQuantity = product.StockQuantity,
                            FailureReason = StockValidationFailureReason.ProductInactive,
                            ErrorMessage = $"{product.Name} is no longer available"
                        });
                        continue;
                    }

                    // Check if completely out of stock
                    if (product.StockQuantity == 0)
                    {
                        _logger.LogWarning("Product is out of stock: {ProductId} ({ProductName})", item.ProductId, product.Name);
                        result.IsValid = false;
                        result.InvalidItems.Add(new StockValidationItem
                        {
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            RequestedQuantity = item.Quantity,
                            AvailableQuantity = 0,
                            FailureReason = StockValidationFailureReason.OutOfStock,
                            ErrorMessage = $"{product.Name} is out of stock"
                        });
                        continue;
                    }

                    // Check if insufficient stock for requested quantity
                    if (product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product: {ProductId} ({ProductName}). Required: {Required}, Available: {Available}",
                            item.ProductId, product.Name, item.Quantity, product.StockQuantity);
                        result.IsValid = false;
                        result.InvalidItems.Add(new StockValidationItem
                        {
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            RequestedQuantity = item.Quantity,
                            AvailableQuantity = product.StockQuantity,
                            FailureReason = StockValidationFailureReason.InsufficientStock,
                            ErrorMessage = $"{product.Name}: Requested {item.Quantity}, but only {product.StockQuantity} available"
                        });
                    }
                }

                // Update overall message if validation failed
                if (!result.IsValid)
                {
                    var itemCount = result.InvalidItems.Count;
                    result.Message = itemCount == 1
                        ? $"1 item is unavailable or out of stock"
                        : $"{itemCount} items are unavailable or out of stock";

                    _logger.LogDebug("Stock validation failed: {Message}", result.Message);
                }
                else
                {
                    _logger.LogDebug("Stock availability validation passed - all items in stock");
                }

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate stock availability with details");
                throw new InvalidOperationException($"Failed to validate stock availability: {ex.Message}", ex);
            }
        }

    }
}
