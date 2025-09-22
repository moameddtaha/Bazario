using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Aggregated order statistics for a store
    /// </summary>
    public class StoreOrderStats
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int RepeatCustomers { get; set; }
        public double CustomerRetentionRate { get; set; }
        public List<MonthlyOrderData> MonthlyData { get; set; } = new();
    }
}
