using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Store analytics data
    /// </summary>
    public class StoreAnalytics
    {
        public Guid StoreId { get; set; }
        public string? StoreName { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int RepeatCustomers { get; set; }
        public double CustomerRetentionRate { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ProductPerformance> TopProducts { get; set; } = new();
        public List<MonthlyStoreData> MonthlyData { get; set; } = new();
        public DateRange AnalyticsPeriod { get; set; } = new();
    }
}
