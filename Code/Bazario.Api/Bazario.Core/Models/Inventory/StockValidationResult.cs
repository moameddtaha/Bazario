using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock validation result
    /// </summary>
    public class StockValidationResult
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string? Message { get; set; }
    }
}
