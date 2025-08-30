using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Product;

namespace Bazario.Core.ServiceContracts
{
    /// <summary>
    /// Service contract for product management operations
    /// Handles product CRUD, inventory management, and product analytics
    /// </summary>
    public interface IProductService
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
        /// Deletes a product if business rules allow
        /// </summary>
        /// <param name="productId">Product ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ProductNotFoundException">Thrown when product is not found</exception>
        /// <exception cref="ProductDeletionNotAllowedException">Thrown when product cannot be deleted due to active orders</exception>
        Task<bool> DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a product by ID with complete details
        /// </summary>
        /// <param name="productId">Product ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product response or null if not found</returns>
        Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all products for a specific store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of store products</returns>
        Task<List<ProductResponse>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches products with advanced filtering and pagination
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        Task<PagedResponse<ProductResponse>> SearchProductsAsync(ProductSearchCriteria searchCriteria, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Gets products with low stock levels
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of products with low stock</returns>
        Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets product analytics including sales, views, ratings
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product analytics data</returns>
        Task<ProductAnalytics> GetProductAnalyticsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a product can be ordered (stock, active status, etc.)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Desired quantity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    }

}
