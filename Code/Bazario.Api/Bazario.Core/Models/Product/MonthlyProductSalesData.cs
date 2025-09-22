using System;

namespace Bazario.Core.Models.Product
{
    /// <summary>
    /// Monthly product sales data for analytics
    /// </summary>
    public class MonthlyProductSalesData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Sales { get; set; }
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
    }
}
