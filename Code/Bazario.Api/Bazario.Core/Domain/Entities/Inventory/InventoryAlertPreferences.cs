using System;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.Domain.Entities.Inventory
{
    /// <summary>
    /// Domain entity representing inventory alert preferences for a store
    /// </summary>
    public class InventoryAlertPreferences
    {
        /// <summary>
        /// Store identifier (Primary Key)
        /// </summary>
        [Key]
        public Guid StoreId { get; set; }

        /// <summary>
        /// Email address to send alerts to
        /// </summary>
        [Required]
        [MaxLength(254)]
        public string AlertEmail { get; set; } = string.Empty;

        /// <summary>
        /// Enable low stock alerts
        /// </summary>
        [Required]
        public bool EnableLowStockAlerts { get; set; } = true;

        /// <summary>
        /// Enable out of stock alerts
        /// </summary>
        [Required]
        public bool EnableOutOfStockAlerts { get; set; } = true;

        /// <summary>
        /// Enable restock recommendations
        /// </summary>
        [Required]
        public bool EnableRestockRecommendations { get; set; } = true;

        /// <summary>
        /// Enable dead stock alerts
        /// </summary>
        [Required]
        public bool EnableDeadStockAlerts { get; set; } = true;

        /// <summary>
        /// Default threshold for low stock alerts
        /// </summary>
        [Required]
        public int DefaultLowStockThreshold { get; set; } = 10;

        /// <summary>
        /// Number of days before stock is considered dead
        /// </summary>
        [Required]
        public int DeadStockDays { get; set; } = 90;

        /// <summary>
        /// Send daily summary email
        /// </summary>
        [Required]
        public bool SendDailySummary { get; set; } = false;

        /// <summary>
        /// Send weekly summary email
        /// </summary>
        [Required]
        public bool SendWeeklySummary { get; set; } = true;

        /// <summary>
        /// Timestamp when preferences were created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when preferences were last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// Automatically managed by EF Core to prevent lost updates during concurrent alert preference modifications
        /// Critical for preventing conflicts when store owners update notification settings
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}