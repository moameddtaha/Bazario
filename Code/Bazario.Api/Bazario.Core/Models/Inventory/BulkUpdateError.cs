using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Bulk update error
    /// </summary>
    public class BulkUpdateError
    {
        public Guid? ProductId { get; set; }
        public string? ProductSku { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
