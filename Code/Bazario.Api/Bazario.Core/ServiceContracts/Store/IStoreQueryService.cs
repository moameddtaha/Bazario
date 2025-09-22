using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Store;
using Bazario.Core.Models.Shared;
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
        /// Gets stores by category with pagination using search criteria
        /// </summary>
        /// <param name="searchCriteria">Search criteria with category filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stores in the specified category</returns>
        Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    }
}
