using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Current inventory status
    /// </summary>
    public class InventoryStatus
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
        public int AvailableStock => CurrentStock - ReservedStock;
        public int LowStockThreshold { get; set; }
        public bool IsLowStock => AvailableStock <= LowStockThreshold;
        public bool IsOutOfStock => AvailableStock <= 0;
        public DateTime LastUpdated { get; set; }
    }
}
