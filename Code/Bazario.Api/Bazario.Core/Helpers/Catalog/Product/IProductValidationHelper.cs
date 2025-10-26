using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.Helpers.Catalog.Product
{
    /// <summary>
    /// Helper interface for product validation operations
    /// </summary>
    public interface IProductValidationHelper
    {
        /// <summary>
        /// Checks if a product can be safely deleted
        /// </summary>
        Task<bool> CanProductBeSafelyDeletedAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has active orders
        /// </summary>
        Task<bool> HasProductActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has pending reservations
        /// </summary>
        Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product has reviews
        /// </summary>
        Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
