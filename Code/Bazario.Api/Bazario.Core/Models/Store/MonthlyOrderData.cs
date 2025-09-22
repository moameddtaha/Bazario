using System;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Monthly order data for analytics
    /// </summary>
    public class MonthlyOrderData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
        public int NewCustomers { get; set; }
        public int ProductsSold { get; set; }
    }
}
