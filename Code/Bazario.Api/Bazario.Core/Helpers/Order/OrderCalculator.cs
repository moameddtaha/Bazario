using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Order
{
    /// <summary>
    /// Helper class for calculating order totals, shipping, and discounts
    /// Follows KISS principle by breaking down complex calculations into focused methods
    /// </summary>
    public class OrderCalculator
    {
        private readonly IProductRepository _productRepository;
        private readonly IShippingZoneService _shippingZoneService;
        private readonly IDiscountRepository _discountRepository;
        private readonly ILogger<OrderCalculator> _logger;

        public OrderCalculator(
            IProductRepository productRepository,
            IShippingZoneService shippingZoneService,
            IDiscountRepository discountRepository,
            ILogger<OrderCalculator> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _shippingZoneService = shippingZoneService ?? throw new ArgumentNullException(nameof(shippingZoneService));
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calculates the subtotal for order items
        /// </summary>
        public async Task<decimal> CalculateSubtotalAsync(
            List<OrderItemAddRequest> orderItems,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Calculating subtotal for {ItemCount} items", orderItems.Count);

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

            _logger.LogDebug("Subtotal calculated: {Subtotal}", subtotal);
            return subtotal;
        }

        /// <summary>
        /// Groups order items by their store ID
        /// </summary>
        public async Task<Dictionary<Guid, List<OrderItemAddRequest>>> GroupItemsByStoreAsync(
            List<OrderItemAddRequest> orderItems,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Grouping items by store");

            var productStoreMap = new Dictionary<Guid, List<OrderItemAddRequest>>();

            foreach (var item in orderItems)
            {
                var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                if (product != null)
                {
                    var storeId = product.StoreId;

                    if (!productStoreMap.ContainsKey(storeId))
                    {
                        productStoreMap[storeId] = new List<OrderItemAddRequest>();
                    }

                    productStoreMap[storeId].Add(item);
                }
            }

            _logger.LogDebug("Found {StoreCount} unique stores in order", productStoreMap.Count);
            return productStoreMap;
        }

        /// <summary>
        /// Calculates total shipping cost for all stores in the order
        /// </summary>
        public async Task<decimal> CalculateShippingCostAsync(
            Dictionary<Guid, List<OrderItemAddRequest>> itemsByStore,
            ShippingAddress shippingAddress,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Calculating shipping cost for {StoreCount} stores", itemsByStore.Count);

            decimal totalShippingCost = 0;

            foreach (var (storeId, items) in itemsByStore)
            {
                var storeShippingCost = await CalculateStoreShippingAsync(
                    storeId,
                    items,
                    shippingAddress,
                    cancellationToken);

                totalShippingCost += storeShippingCost;

                _logger.LogDebug("Store {StoreId} shipping cost: {ShippingCost}", storeId, storeShippingCost);
            }

            _logger.LogDebug("Total shipping cost: {TotalShippingCost}", totalShippingCost);
            return totalShippingCost;
        }

        /// <summary>
        /// Calculates shipping cost for a single store
        /// </summary>
        private async Task<decimal> CalculateStoreShippingAsync(
            Guid storeId,
            List<OrderItemAddRequest> storeItems,
            ShippingAddress shippingAddress,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get store-specific delivery fee
                var deliveryFee = await _shippingZoneService.GetStoreDeliveryFeeAsync(
                    storeId,
                    shippingAddress.City,
                    "EG",
                    cancellationToken);

                // Determine shipping zone to check if supported
                var shippingZone = await _shippingZoneService.DetermineStoreShippingZoneAsync(
                    storeId,
                    shippingAddress.City,
                    "EG",
                    cancellationToken);

                if (shippingZone == ShippingZone.NotSupported)
                {
                    throw new InvalidOperationException(
                        $"Shipping is not supported to the address: {shippingAddress.City}, {shippingAddress.State}. " +
                        "Only Egyptian addresses are currently supported.");
                }

                return deliveryFee;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine store-specific shipping for store {StoreId}, using fallback", storeId);

                // Fallback to simple zone determination
                var fallbackZone = GetSimpleFallbackZone(shippingAddress.City, "EG");
                var fallbackFee = GetSimpleDeliveryFee(fallbackZone);

                _logger.LogDebug("Using fallback shipping zone: {ShippingZone}, fee: {Fee} for store {StoreId}",
                    fallbackZone, fallbackFee, storeId);

                return fallbackFee;
            }
        }

        /// <summary>
        /// Calculates total discount amount from multiple discount codes
        /// </summary>
        public async Task<(decimal TotalDiscount, List<string> AppliedDiscounts, List<string> AppliedDiscountTypes)>
            CalculateDiscountsAsync(
                List<string>? discountCodes,
                decimal subtotal,
                List<Guid> storeIds,
                CancellationToken cancellationToken = default)
        {
            if (discountCodes == null || !discountCodes.Any())
            {
                _logger.LogDebug("No discount codes provided");
                return (0, new List<string>(), new List<string>());
            }

            _logger.LogDebug("Validating {DiscountCount} discount codes: {DiscountCodes}",
                discountCodes.Count, string.Join(", ", discountCodes));

            decimal totalDiscountAmount = 0;
            var appliedDiscounts = new List<string>();
            var appliedDiscountTypes = new List<string>();

            foreach (var discountCode in discountCodes)
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                    continue;

                var (discountAmount, discountInfo) = await ApplySingleDiscountAsync(
                    discountCode,
                    subtotal,
                    totalDiscountAmount,
                    storeIds,
                    cancellationToken);

                if (discountAmount > 0 && discountInfo != null)
                {
                    totalDiscountAmount += discountAmount;
                    appliedDiscounts.Add($"{discountInfo.Value.Code} ({discountInfo.Value.Type})");
                    appliedDiscountTypes.Add(discountInfo.Value.Type.ToString());

                    _logger.LogDebug("Applied discount: {DiscountCode}, Amount: {DiscountAmount}",
                        discountCode, discountAmount);
                }
            }

            _logger.LogDebug("Total discount amount: {TotalDiscount}", totalDiscountAmount);
            return (totalDiscountAmount, appliedDiscounts, appliedDiscountTypes);
        }

        /// <summary>
        /// Applies a single discount code
        /// </summary>
        private async Task<(decimal Amount, (string Code, DiscountType Type)? Info)> ApplySingleDiscountAsync(
            string discountCode,
            decimal subtotal,
            decimal alreadyAppliedDiscount,
            List<Guid> storeIds,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Validating discount code: {DiscountCode}", discountCode);

            var (isValid, discount, errorMessage) = await _discountRepository.ValidateDiscountAsync(
                discountCode,
                subtotal,
                storeIds,
                cancellationToken);

            if (!isValid || discount == null)
            {
                _logger.LogWarning("Invalid discount code: {DiscountCode}, Error: {ErrorMessage}",
                    discountCode, errorMessage);
                return (0, null);
            }

            // Calculate discount amount based on type
            decimal discountAmount = discount.Type == DiscountType.Percentage
                ? subtotal * discount.Value  // Value is already a decimal (0.10 = 10%)
                : discount.Value;            // Value is the fixed amount in EGP

            // Ensure discount doesn't exceed remaining subtotal
            discountAmount = Math.Min(discountAmount, subtotal - alreadyAppliedDiscount);

            if (discountAmount <= 0)
            {
                return (0, null);
            }

            return (discountAmount, (discount.Code, discount.Type));
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
