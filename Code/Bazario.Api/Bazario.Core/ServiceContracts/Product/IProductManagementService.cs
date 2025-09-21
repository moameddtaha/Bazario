using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Product;

namespace Bazario.Core.ServiceContracts.Product
{
    /// <summary>
    /// Service contract for product management operations (CRUD)
    /// Handles product creation, updates, and deletion
    /// </summary>
    public interface IProductManagementService
    {
        /// <summary>
        /// Creates a new product with validation and business rules
        /// </summary>
        /// <param name="productAddRequest">Product creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created product response</returns>
        /// <exception cref="ArgumentNullException">Thrown when productAddRequest is null</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="StoreNotFoundException">Thrown when store is not found</exception>
        Task<ProductResponse> CreateProductAsync(ProductAddRequest productAddRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing product with validation
        /// </summary>
        /// <param name="productUpdateRequest">Product update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated product response</returns>
        /// <exception cref="ArgumentNullException">Thrown when productUpdateRequest is null</exception>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a product (marks as deleted but preserves data)
        /// </summary>
        /// <param name="productId">Product ID to delete</param>
        /// <param name="deletedBy">User ID performing the deletion</param>
        /// <param name="reason">Reason for deletion (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        Task<bool> DeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hard deletes a product (permanently removes from database)
        /// Requires admin privileges and no existing orders
        /// </summary>
        /// <param name="productId">Product ID to delete</param>
        /// <param name="deletedBy">Admin user ID performing the deletion</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not admin</exception>
        /// <exception cref="ProductDeletionNotAllowedException">Thrown when product cannot be deleted due to active orders</exception>
        Task<bool> HardDeleteProductAsync(Guid productId, Guid deletedBy, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted product
        /// </summary>
        /// <param name="productId">Product ID to restore</param>
        /// <param name="restoredBy">User ID performing the restoration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restored product response</returns>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        Task<ProductResponse> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default);
    }
}
