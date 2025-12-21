using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Aggregated statistics for orders using a specific discount code.
    /// Used for bulk analytics queries to avoid N+1 query problems.
    /// Contains only aggregated data - no full Order entities loaded.
    /// </summary>
    public class OrderDiscountStats
    {
        /// <summary>
        /// The discount code these statistics apply to
        /// </summary>
        public string DiscountCode { get; set; } = string.Empty;

        /// <summary>
        /// Total number of orders that used this discount code
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Sum of all discount amounts applied for this code
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Average discount amount per order for this code
        /// </summary>
        public decimal AverageDiscountAmount { get; set; }

        /// <summary>
        /// Total revenue (order totals) from orders using this code
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Average order value for orders using this code
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Date of first order using this discount code (null if never used)
        /// </summary>
        public DateTime? FirstUsed { get; set; }

        /// <summary>
        /// Date of most recent order using this discount code (null if never used)
        /// </summary>
        public DateTime? LastUsed { get; set; }
    }
}
