using System;
using Bazario.Core.Enums;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Order performance metrics
    /// </summary>
    public class OrderPerformanceMetrics
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal AverageProcessingTime { get; set; } // in hours
        public decimal AverageDeliveryTime { get; set; } // in hours
        public decimal OrderFulfillmentRate { get; set; } // percentage
        public decimal CancellationRate { get; set; } // percentage
    }
}
