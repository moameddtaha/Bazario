using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory alerts and notifications
    /// Handles low stock alerts, expiration warnings, and inventory notifications
    /// </summary>
    public interface IInventoryAlertService
    {
        /// <summary>
        /// Sends low stock alert for a product
        /// </summary>
        Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends out of stock notification
        /// </summary>
        Task SendOutOfStockNotificationAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends bulk low stock alerts for multiple products
        /// </summary>
        Task SendBulkLowStockAlertsAsync(List<LowStockAlert> alerts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends restock recommendation
        /// </summary>
        Task SendRestockRecommendationAsync(Guid productId, int recommendedQuantity, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes and sends all pending inventory alerts
        /// </summary>
        Task<int> ProcessPendingAlertsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures alert preferences for a store
        /// </summary>
        Task<bool> ConfigureAlertPreferencesAsync(Guid storeId, InventoryAlertPreferences preferences, CancellationToken cancellationToken = default);
    }
}
