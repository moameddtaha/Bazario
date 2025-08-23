using System;
using System.Collections.Generic;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory report request
    /// </summary>
    public class InventoryReportRequest
    {
        public Guid? StoreId { get; set; }
        public List<Guid>? ProductIds { get; set; }
        public DateRange? DateRange { get; set; }
        public InventoryReportType ReportType { get; set; } = InventoryReportType.Current;
        public bool IncludeLowStock { get; set; } = true;
        public bool IncludeMovements { get; set; } = false;
    }
}
