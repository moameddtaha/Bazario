using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Inventory alert preferences for a store
    /// </summary>
    public class InventoryAlertPreferences
    {
        public Guid StoreId { get; set; }
        public bool EnableLowStockAlerts { get; set; } = true;
        public bool EnableOutOfStockAlerts { get; set; } = true;
        public bool EnableRestockRecommendations { get; set; } = true;
        public bool EnableDeadStockAlerts { get; set; } = true;
        public int DefaultLowStockThreshold { get; set; } = 10;
        public int DeadStockDays { get; set; } = 90;
        public string AlertEmail { get; set; } = string.Empty;
        public bool SendDailySummary { get; set; } = false;
        public bool SendWeeklySummary { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// Required for safe concurrent updates to alert preferences
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
