using System;

namespace Bazario.Core.Models.Store
{
    public class OrderStatsProjection
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime Date { get; set; }
        public decimal StoreRevenue { get; set; }
        public int StoreProductsSold { get; set; }
    }
}
