using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Dead stock analysis item (slow-moving inventory)
    /// </summary>
    public class DeadStockItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTime? LastSaleDate { get; set; }
        public int DaysSinceLastSale { get; set; }
        public decimal StockValue { get; set; }
        public decimal UnitPrice { get; set; }
        public string Recommendation { get; set; } = string.Empty; // e.g., "Discount", "Bundle", "Clear Out"
        public DateTime AnalyzedAt { get; set; }
    }
}
