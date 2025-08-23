using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Bulk stock item
    /// </summary>
    public class BulkStockItem
    {
        public Guid ProductId { get; set; }
        public string? ProductSku { get; set; }
        public int NewQuantity { get; set; }
    }
}
