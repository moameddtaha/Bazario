using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Validation operations for store business rules
    /// </summary>
    public interface IStoreValidationService
    {
        /// <summary>
        /// Validates if a store can be created for a seller
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <param name="storeName">Proposed store name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a store can be updated
        /// </summary>
        /// <param name="storeId">Store ID to update</param>
        /// <param name="sellerId">Seller ID (must be the owner)</param>
        /// <param name="newStoreName">New store name (null if not changing)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<StoreValidationResult> ValidateStoreUpdateAsync(Guid storeId, Guid sellerId, string? newStoreName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a store can be deleted
        /// </summary>
        /// <param name="storeId">Store ID to delete</param>
        /// <param name="sellerId">Seller ID (must be the owner)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<StoreValidationResult> ValidateStoreDeletionAsync(Guid storeId, Guid sellerId, CancellationToken cancellationToken = default);
    }
}
