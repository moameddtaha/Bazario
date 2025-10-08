using System;

namespace Bazario.Core.Models.Catalog.Product
{
    /// <summary>
    /// Monthly sales data for analytics
    /// </summary>
    public class MonthlySalesData
    {
        public string Month { get; set; } = string.Empty; // Format: "yyyy-MM"
        public int Sales { get; set; }
        public int Year { get; set; }
        public int MonthNumber { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
