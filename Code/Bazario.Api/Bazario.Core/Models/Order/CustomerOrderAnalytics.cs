using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Customer order analytics data
    /// </summary>
    public class CustomerOrderAnalytics
    {
        public Guid CustomerId { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime? FirstOrderDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public List<MonthlyOrderData> MonthlyData { get; set; } = new();
    }
}
