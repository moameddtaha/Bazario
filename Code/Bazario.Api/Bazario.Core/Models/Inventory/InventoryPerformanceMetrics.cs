using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory performance metrics for a store
    /// </summary>
    public class InventoryPerformanceMetrics
    {
        public Guid StoreId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Stock metrics
        public decimal AverageTurnoverRate { get; set; }
        public int StockoutOccurrences { get; set; }
        public decimal StockoutRate { get; set; }
        
        // Financial metrics
        public decimal TotalStockValue { get; set; }
        public decimal AverageInventoryHoldingCost { get; set; }
        
        // Efficiency metrics
        public int DaysOfInventoryOnHand { get; set; }
        public decimal InventoryAccuracy { get; set; }
        public int TotalStockMovements { get; set; }
        
        // Alert metrics
        public int LowStockAlerts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int OverstockedProducts { get; set; }
        
        public DateTime CalculatedAt { get; set; }
    }
}
