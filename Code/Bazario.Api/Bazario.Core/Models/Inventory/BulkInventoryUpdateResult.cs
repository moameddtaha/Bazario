using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Bulk update result
    /// </summary>
    public class BulkInventoryUpdateResult
    {
        public int TotalItems { get; set; }
        public int SuccessfulUpdates { get; set; }
        public int FailedUpdates { get; set; }
        public List<BulkUpdateError> Errors { get; set; } = new();
    }
}
