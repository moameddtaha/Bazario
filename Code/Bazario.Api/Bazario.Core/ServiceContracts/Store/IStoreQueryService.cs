using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Read operations and queries for stores
    /// </summary>
    public interface IStoreQueryService
    {
        /// <summary>
        /// Retrieves a store by ID with complete details
        /// </summary>
        /// <param name="storeId">Store ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store response or null if not found</returns>
        Task<StoreResponse?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all stores for a specific seller
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of seller stores</returns>
        Task<List<StoreResponse>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches stores with filtering and pagination
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        Task<PagedResponse<StoreResponse>> SearchStoresAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stores by category with pagination
        /// </summary>
        /// <param name="category">Store category</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stores in the specified category</returns>
        Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }
}
