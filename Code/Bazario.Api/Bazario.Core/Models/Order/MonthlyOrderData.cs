using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Monthly order data for analytics
    /// </summary>
    public class MonthlyOrderData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Orders { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
