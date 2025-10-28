using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Catalog.Product;

namespace Bazario.Core.ServiceContracts.Catalog.Product
{
    /// <summary>
    /// Service contract for product validation operations
    /// Handles product validation logic and business rules
    /// </summary>
    public interface IProductValidationService
    {
        /// <summary>
        /// Validates if a product can be ordered (stock, active status, etc.)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Desired quantity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product can be safely deleted
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product can be safely deleted, false otherwise</returns>
        Task<bool> CanProductBeSafelyDeletedAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any active orders
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has active orders, false otherwise</returns>
        Task<bool> HasProductActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any pending reservations
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has pending reservations, false otherwise</returns>
        Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any reviews
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has reviews, false otherwise</returns>
        Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
