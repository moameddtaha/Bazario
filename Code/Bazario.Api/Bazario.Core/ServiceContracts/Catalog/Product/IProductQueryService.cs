using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Catalog.Product
{
    /// <summary>
    /// Service contract for product query operations
    /// Handles product retrieval, searching, and filtering
    /// </summary>
    public interface IProductQueryService
    {
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
        /// Gets products with low stock levels
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of products with low stock</returns>
        Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default);
    }
}
