using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory update result
    /// </summary>
    public class InventoryUpdateResult
    {
        public bool IsSuccessful { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public Guid MovementId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
