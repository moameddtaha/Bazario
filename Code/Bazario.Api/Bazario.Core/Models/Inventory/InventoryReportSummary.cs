using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory report summary
    /// </summary>
    public class InventoryReportSummary
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int TotalMovements { get; set; }
    }
}
