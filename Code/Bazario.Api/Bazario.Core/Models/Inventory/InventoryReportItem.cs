using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory report item
    /// </summary>
    public class InventoryReportItem
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Sku { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
        public int AvailableStock { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime LastMovement { get; set; }
    }
}
