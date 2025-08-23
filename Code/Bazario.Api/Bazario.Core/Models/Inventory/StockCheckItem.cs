using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock check item for validation
    /// </summary>
    public class StockCheckItem
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
    }
}
