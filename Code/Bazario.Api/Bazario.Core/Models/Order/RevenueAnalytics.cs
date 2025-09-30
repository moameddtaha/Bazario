using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Revenue analytics data
    /// </summary>
    public class RevenueAnalytics
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal HighestOrderValue { get; set; }
        public decimal LowestOrderValue { get; set; }
        public List<MonthlyRevenueData> MonthlyData { get; set; } = new();
    }
}
