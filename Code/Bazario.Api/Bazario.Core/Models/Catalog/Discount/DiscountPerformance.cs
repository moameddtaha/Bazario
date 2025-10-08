using System;
using System.Collections.Generic;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.Models.Catalog.Discount
{
    /// <summary>
    /// Performance metrics for discount codes
    /// </summary>
    public class DiscountPerformance
    {
        /// <summary>
        /// Discount code
        /// </summary>
        public string DiscountCode { get; set; } = string.Empty;

        /// <summary>
        /// Type of discount (Percentage or FixedAmount)
        /// </summary>
        public DiscountType DiscountType { get; set; }

        /// <summary>
        /// Discount value (percentage as decimal or fixed amount)
        /// </summary>
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Number of orders that used this discount
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Total revenue from orders using this discount
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total discount amount given
        /// </summary>
        public decimal TotalDiscountGiven { get; set; }

        /// <summary>
        /// Net revenue after discounts (TotalRevenue - TotalDiscountGiven)
        /// </summary>
        public decimal NetRevenue { get; set; }

        /// <summary>
        /// Conversion rate - percentage of orders that used this discount
        /// </summary>
        public decimal ConversionRate { get; set; }

        /// <summary>
        /// Average order value for orders using this discount
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Average discount amount per order
        /// </summary>
        public decimal AverageDiscountPerOrder { get; set; }

        /// <summary>
        /// Date range this performance data covers
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date range this performance data covers
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Store ID this discount applies to (null for global discounts)
        /// </summary>
        public Guid? StoreId { get; set; }

        /// <summary>
        /// Store name (if applicable)
        /// </summary>
        public string? StoreName { get; set; }
    }
}
