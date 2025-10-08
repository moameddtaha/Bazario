using System;
using System.Collections.Generic;
using Bazario.Core.Models.Store;
using Bazario.Core.Models.Shared;
using Bazario.Core.Enums.Inventory;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory report
    /// </summary>
    public class InventoryReport
    {
        public InventoryReportType ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateRange? DateRange { get; set; }
        public InventoryReportSummary Summary { get; set; } = new();
        public List<InventoryReportItem> Items { get; set; } = new();
        public List<InventoryMovement> Movements { get; set; } = new();
    }
}
