using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Aggregated statistics for analyzing revenue impact of discounts.
    /// Used for bulk analytics queries to avoid loading full Order entities.
    /// Contains only aggregated data from database-level GROUP BY operations.
    /// </summary>
    /// <remarks>
    /// This DTO is used by both DiscountAnalyticsService and OrderAnalyticsService
    /// for efficient revenue impact analysis across all orders in a date range.
    /// Eliminates the need to load 10,000+ full Order entities with navigation properties.
    /// </remarks>
    public class RevenueImpactStats
    {
        /// <summary>
        /// Total revenue from all orders (discounted + non-discounted)
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total amount of discounts given
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Total count of all orders
        /// </summary>
        public int TotalOrderCount { get; set; }

        /// <summary>
        /// Count of orders with discounts applied
        /// </summary>
        public int DiscountedOrderCount { get; set; }

        /// <summary>
        /// Count of orders without discounts
        /// </summary>
        public int NonDiscountedOrderCount { get; set; }

        /// <summary>
        /// Total revenue from discounted orders only
        /// </summary>
        public decimal DiscountedOrderRevenue { get; set; }

        /// <summary>
        /// Total revenue from non-discounted orders only
        /// </summary>
        public decimal NonDiscountedOrderRevenue { get; set; }

        /// <summary>
        /// Average order value for discounted orders
        /// </summary>
        public decimal AverageDiscountedOrderValue { get; set; }

        /// <summary>
        /// Average order value for non-discounted orders
        /// </summary>
        public decimal AverageNonDiscountedOrderValue { get; set; }
    }
}
