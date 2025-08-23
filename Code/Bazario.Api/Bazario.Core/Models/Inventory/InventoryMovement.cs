using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory movement record
    /// </summary>
    public class InventoryMovement
    {
        public Guid MovementId { get; set; }
        public Guid ProductId { get; set; }
        public StockUpdateType Type { get; set; }
        public int PreviousQuantity { get; set; }
        public int QuantityChanged { get; set; }
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
        public Guid UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Reference { get; set; }
    }
}
