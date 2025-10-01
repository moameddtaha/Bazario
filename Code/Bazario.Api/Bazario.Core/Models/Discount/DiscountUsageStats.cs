using System;

namespace Bazario.Core.Models.Discount
{
    /// <summary>
    /// Statistics about discount code usage
    /// </summary>
    public class DiscountUsageStats
    {
        /// <summary>
        /// Discount code
        /// </summary>
        public string DiscountCode { get; set; } = string.Empty;

        /// <summary>
        /// Total number of times this discount has been used
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Total discount amount given across all uses
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Average discount amount per use
        /// </summary>
        public decimal AverageDiscountAmount { get; set; }

        /// <summary>
        /// Total revenue from orders that used this discount
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Average order value for orders using this discount
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// First time this discount was used
        /// </summary>
        public DateTime? FirstUsed { get; set; }

        /// <summary>
        /// Most recent time this discount was used
        /// </summary>
        public DateTime? LastUsed { get; set; }

        /// <summary>
        /// Whether this discount is currently active
        /// </summary>
        public bool IsActive { get; set; }

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
