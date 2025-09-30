using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory turnover data for analytics
    /// </summary>
    public class InventoryTurnoverData
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TurnoverRate { get; set; }
        public int TotalSold { get; set; }
        public decimal AverageStockLevel { get; set; }
        public int DaysToSell { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
