using System;

namespace Bazario.Core.Models.Product
{
    /// <summary>
    /// Monthly sales data for analytics
    /// </summary>
    public class MonthlySalesData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
