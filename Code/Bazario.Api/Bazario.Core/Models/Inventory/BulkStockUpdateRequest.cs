using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Bulk stock update request
    /// </summary>
    public class BulkStockUpdateRequest
    {
        public List<BulkStockItem> Items { get; set; } = new();
        public StockUpdateType UpdateType { get; set; }
        public string? Reason { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
