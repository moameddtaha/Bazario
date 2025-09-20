using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
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
        /// Deletes a store if business rules allow
        /// </summary>
        /// <param name="storeId">Store ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found or cannot be deleted</exception>
        Task<bool> DeleteStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

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
