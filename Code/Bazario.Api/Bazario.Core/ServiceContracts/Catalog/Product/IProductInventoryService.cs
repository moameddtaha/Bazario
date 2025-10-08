using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Catalog.Product;

namespace Bazario.Core.ServiceContracts.Catalog.Product
{
    /// <summary>
    /// Service contract for product inventory management
    /// Handles stock updates, reservations, and releases
    /// </summary>
    public interface IProductInventoryService
    {
        /// <summary>
        /// Updates product stock quantity with validation
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="newQuantity">New stock quantity</param>
        /// <param name="reason">Reason for stock update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated product response</returns>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        /// <exception cref="InvalidStockQuantityException">Thrown when quantity is invalid</exception>
        Task<ProductResponse> UpdateStockAsync(Guid productId, int newQuantity, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reserves product stock for an order
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Quantity to reserve</param>
        /// <param name="orderId">Order ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully reserved</returns>
        /// <exception cref="InsufficientStockException">Thrown when not enough stock available</exception>
        Task<bool> ReserveStockAsync(Guid productId, int quantity, Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases reserved stock (e.g., when order is cancelled)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Quantity to release</param>
        /// <param name="orderId">Order ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully released</returns>
        Task<bool> ReleaseStockAsync(Guid productId, int quantity, Guid orderId, CancellationToken cancellationToken = default);
    }
}
