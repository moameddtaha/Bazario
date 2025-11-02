using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory alerts and notifications for e-commerce inventory management
    /// </summary>
    /// <remarks>
    /// Provides 6 core methods for inventory alert management:
    /// - SendLowStockAlertAsync: Sends email when product stock falls below threshold
    /// - SendOutOfStockNotificationAsync: Sends urgent email when product is completely out of stock
    /// - SendBulkLowStockAlertsAsync: Sends consolidated email for multiple low stock products
    /// - SendRestockRecommendationAsync: Sends restock suggestions based on analytics
    /// - ProcessPendingAlertsAsync: Batch processes alerts for all configured stores
    /// - ConfigureAlertPreferencesAsync: Configures alert preferences per store
    ///
    /// PERSISTENCE: Alert preferences are persisted to the database and cached in memory for performance.
    /// Uses a 3-layer cache-aside pattern: Memory Cache -> Database -> Configuration Defaults.
    /// Cache invalidation is handled automatically on updates. Preferences survive application restarts.
    ///
    /// Thread-safety: Uses double-check locking with per-store semaphores to prevent cache stampede.
    /// All methods log exceptions but do not rethrow - callers should check email send success.
    /// </remarks>
    public interface IInventoryAlertService
    {
        /// <summary>
        /// Sends a low stock alert email when product inventory falls below threshold
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="currentStock">Current stock level</param>
        /// <param name="threshold">Low stock threshold that was crossed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if alert was sent successfully, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when productId is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when currentStock or threshold is negative</exception>
        Task<bool> SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an urgent out-of-stock notification email when product inventory reaches zero
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if notification was sent successfully, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when productId is empty</exception>
        Task<bool> SendOutOfStockNotificationAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends consolidated low stock alert emails grouped by store for efficient processing
        /// </summary>
        /// <param name="alerts">List of low stock alerts to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentNullException">Thrown when alerts list is null</exception>
        /// <remarks>
        /// Alerts are automatically grouped by store ID and sent in parallel for performance.
        /// Empty lists are handled gracefully without sending emails.
        /// </remarks>
        Task SendBulkLowStockAlertsAsync(List<LowStockAlert> alerts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a restock recommendation email with suggested quantity and reasoning
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="recommendedQuantity">Recommended quantity to restock</param>
        /// <param name="reason">Reason for the recommendation (e.g., based on sales patterns)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if recommendation was sent successfully, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when productId is empty or reason is null/empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when recommendedQuantity is not positive</exception>
        Task<bool> SendRestockRecommendationAsync(Guid productId, int recommendedQuantity, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch processes all pending inventory alerts for stores with configured preferences
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of alerts successfully processed, or null if an error occurred</returns>
        /// <remarks>
        /// Processes both low stock alerts (sent in bulk) and out-of-stock notifications (sent individually).
        /// Only processes alerts for stores that have alert preferences configured.
        /// Returns null to indicate error, 0 to indicate no alerts processed successfully.
        /// </remarks>
        Task<int?> ProcessPendingAlertsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures alert preferences for a specific store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="preferences">Alert preferences configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration succeeded, false otherwise</returns>
        /// <remarks>
        /// Preferences are persisted to the database and cached for performance.
        /// Cache is invalidated before database write to prevent stale data race conditions.
        /// Preferences will survive application restarts.
        /// </remarks>
        Task<bool> ConfigureAlertPreferencesAsync(Guid storeId, Bazario.Core.Domain.Entities.Inventory.InventoryAlertPreferences preferences, CancellationToken cancellationToken = default);
    }
}
