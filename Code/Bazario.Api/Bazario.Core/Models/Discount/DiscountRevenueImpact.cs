using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Discount
{
    /// <summary>
    /// Analysis of discount impact on revenue
    /// </summary>
    public class DiscountRevenueImpact
    {
        /// <summary>
        /// Total revenue from all orders in the period
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Revenue from orders that used discounts
        /// </summary>
        public decimal DiscountedOrderRevenue { get; set; }

        /// <summary>
        /// Revenue from orders without discounts
        /// </summary>
        public decimal NonDiscountedOrderRevenue { get; set; }

        /// <summary>
        /// Total discount amount given across all orders
        /// </summary>
        public decimal TotalDiscountsGiven { get; set; }

        /// <summary>
        /// Net revenue after all discounts
        /// </summary>
        public decimal NetRevenue { get; set; }

        /// <summary>
        /// Percentage of revenue that came from discounted orders
        /// </summary>
        public decimal DiscountedRevenuePercentage { get; set; }

        /// <summary>
        /// Average order value for discounted orders
        /// </summary>
        public decimal AverageDiscountedOrderValue { get; set; }

        /// <summary>
        /// Average order value for non-discounted orders
        /// </summary>
        public decimal AverageNonDiscountedOrderValue { get; set; }

        /// <summary>
        /// Number of orders with discounts
        /// </summary>
        public int DiscountedOrderCount { get; set; }

        /// <summary>
        /// Number of orders without discounts
        /// </summary>
        public int NonDiscountedOrderCount { get; set; }

        /// <summary>
        /// Total number of orders
        /// </summary>
        public int TotalOrderCount { get; set; }

        /// <summary>
        /// Discount usage rate (percentage of orders that used discounts)
        /// </summary>
        public decimal DiscountUsageRate { get; set; }

        /// <summary>
        /// Date range this analysis covers
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date range this analysis covers
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Breakdown by discount code
        /// </summary>
        public List<DiscountRevenueBreakdown> DiscountBreakdowns { get; set; } = new();
    }

    /// <summary>
    /// Revenue breakdown for a specific discount code
    /// </summary>
    public class DiscountRevenueBreakdown
    {
        /// <summary>
        /// Discount code
        /// </summary>
        public string DiscountCode { get; set; } = string.Empty;

        /// <summary>
        /// Number of orders using this discount
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Revenue from orders using this discount
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Total discount amount given for this code
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Net revenue for this discount code
        /// </summary>
        public decimal NetRevenue { get; set; }

        /// <summary>
        /// Average order value for this discount
        /// </summary>
        public decimal AverageOrderValue { get; set; }
    }
}
