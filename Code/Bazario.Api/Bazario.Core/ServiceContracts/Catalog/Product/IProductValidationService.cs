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
        /// Checks if a product can be safely deleted without violating referential integrity
        /// </summary>
        /// <remarks>
        /// A product can be safely deleted if:
        /// - It has no active orders (Pending, Processing, Shipped)
        /// - It has no pending reservations within the last 7 days
        /// - Reviews are checked but not a blocking condition (warning only)
        ///
        /// Fail-Safe Strategy:
        /// On error, returns false to prevent deletion, ensuring data integrity is maintained
        /// </remarks>
        /// <param name="productId">Product ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product can be safely deleted, false otherwise</returns>
        Task<bool> CanProductBeSafelyDeletedAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any active orders that would prevent deletion
        /// </summary>
        /// <remarks>
        /// Active orders include: Pending, Processing, and Shipped statuses
        ///
        /// Fail-Safe Strategy:
        /// On error, returns true (assumes product has active orders) to prevent unsafe deletion
        /// </remarks>
        /// <param name="productId">Product ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has active orders, false otherwise</returns>
        Task<bool> HasProductActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any pending reservations (recent Pending/Processing orders)
        /// </summary>
        /// <remarks>
        /// Pending reservations are orders in Pending or Processing status from the last 7 days
        ///
        /// Fail-Safe Strategy:
        /// On error, returns true (assumes product has reservations) to prevent unsafe deletion
        /// </remarks>
        /// <param name="productId">Product ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has pending reservations, false otherwise</returns>
        Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has any reviews
        /// </summary>
        /// <remarks>
        /// Uses the Reviews repository to get the actual count of reviews for the product
        ///
        /// Fail-Safe Strategy:
        /// On error, returns false (assumes no reviews) to allow the operation to proceed
        /// Reviews are not a blocking condition for deletion, only a warning
        /// </remarks>
        /// <param name="productId">Product ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product has reviews, false otherwise</returns>
        Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
