using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;

namespace Bazario.Core.Domain.RepositoryContracts.Inventory
{
    /// <summary>
    /// Repository contract for managing inventory alert preferences
    /// </summary>
    public interface IInventoryAlertPreferencesRepository
    {
        /// <summary>
        /// Gets alert preferences for a specific store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Alert preferences or null if not configured</returns>
        Task<InventoryAlertPreferences?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates alert preferences for a store
        /// </summary>
        /// <param name="preferences">Alert preferences to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Saved preferences</returns>
        Task<InventoryAlertPreferences> UpsertAsync(InventoryAlertPreferences preferences, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes alert preferences for a specific store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if alert preferences exist for a store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if preferences exist</returns>
        Task<bool> ExistsAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
