using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Store;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Core CRUD operations for stores
    /// </summary>
    public interface IStoreManagementService
    {
        /// <summary>
        /// Creates a new store with validation and business rules
        /// </summary>
        /// <param name="storeAddRequest">Store creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created store response</returns>
        /// <exception cref="ArgumentNullException">Thrown when storeAddRequest is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when validation fails or seller not found</exception>
        Task<StoreResponse> CreateStoreAsync(StoreAddRequest storeAddRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing store with validation
        /// </summary>
        /// <param name="storeUpdateRequest">Store update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store response</returns>
        /// <exception cref="ArgumentNullException">Thrown when storeUpdateRequest is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when store not found or validation fails</exception>
        Task<StoreResponse> UpdateStoreAsync(StoreUpdateRequest storeUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a store if business rules allow (standard deletion)
        /// </summary>
        /// <param name="storeId">Store ID to delete</param>
        /// <param name="deletedBy">ID of user performing the deletion</param>
        /// <param name="reason">Reason for deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found or cannot be deleted</exception>
        Task<bool> DeleteStoreAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hard deletes a store permanently (requires admin privileges)
        /// WARNING: This action is irreversible and will permanently remove all store data
        /// </summary>
        /// <param name="storeId">Store ID to permanently delete</param>
        /// <param name="deletedBy">ID of admin user performing the deletion</param>
        /// <param name="reason">Required reason for permanent deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin privileges</exception>
        Task<bool> HardDeleteStoreAsync(Guid storeId, Guid deletedBy, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted store
        /// </summary>
        /// <param name="storeId">Store ID to restore</param>
        /// <param name="restoredBy">ID of user performing the restoration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restored store response</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found or not deleted</exception>
        Task<StoreResponse> RestoreStoreAsync(Guid storeId, Guid restoredBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates store status (active/inactive)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="isActive">New active status</param>
        /// <param name="reason">Reason for status change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store response</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found</exception>
        Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, bool isActive, string? reason = null, CancellationToken cancellationToken = default);
    }
}
