using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.Models.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service for calculating order totals, shipping, and discounts
    /// </summary>
    public interface IOrderCalculationService
    {
        /// <summary>
        /// Calculates the subtotal for order items
        /// </summary>
        Task<decimal> CalculateSubtotalAsync(
            List<OrderItemAddRequest> orderItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Groups order items by their store ID
        /// </summary>
        Task<Dictionary<Guid, List<OrderItemAddRequest>>> GroupItemsByStoreAsync(
            List<OrderItemAddRequest> orderItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates total shipping cost for all stores in the order
        /// </summary>
        Task<decimal> CalculateShippingCostAsync(
            Dictionary<Guid, List<OrderItemAddRequest>> itemsByStore,
            ShippingAddress shippingAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates total discount amount from multiple discount codes
        /// </summary>
        Task<(decimal TotalDiscount, List<string> AppliedDiscounts, List<string> AppliedDiscountTypes)>
            CalculateDiscountsAsync(
                List<string>? discountCodes,
                decimal subtotal,
                List<Guid> storeIds,
                CancellationToken cancellationToken = default);
    }
}
