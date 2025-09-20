using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Store;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts
{
    /// <summary>
    /// Service contract for store management operations
    /// Handles store CRUD, analytics, and business logic
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Creates a new store with validation and business rules
        /// </summary>
        /// <param name="storeAddRequest">Store creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created store response</returns>
        /// <exception cref="ArgumentNullException">Thrown when storeAddRequest is null</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="SellerNotFoundException">Thrown when seller is not found</exception>
        /// <exception cref="DuplicateStoreNameException">Thrown when store name already exists for seller</exception>
        Task<StoreResponse> CreateStoreAsync(StoreAddRequest storeAddRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing store with validation
        /// </summary>
        /// <param name="storeUpdateRequest">Store update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store response</returns>
        /// <exception cref="ArgumentNullException">Thrown when storeUpdateRequest is null</exception>
        /// <exception cref="StoreNotFoundException">Thrown when store is not found</exception>
        Task<StoreResponse> UpdateStoreAsync(StoreUpdateRequest storeUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a store if business rules allow
        /// </summary>
        /// <param name="storeId">Store ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="StoreNotFoundException">Thrown when store is not found</exception>
        /// <exception cref="StoreDeletionNotAllowedException">Thrown when store cannot be deleted due to active products/orders</exception>
        Task<bool> DeleteStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

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
        /// Gets comprehensive analytics for a store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="dateRange">Date range for analytics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store analytics data</returns>
        Task<StoreAnalytics> GetStoreAnalyticsAsync(Guid storeId, DateRange? dateRange = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets store performance summary
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store performance data</returns>
        Task<StorePerformance> GetStorePerformanceAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top performing stores with pagination
        /// </summary>
        /// <param name="criteria">Performance criteria</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Top performing stores</returns>
        Task<PagedResponse<StorePerformance>> GetTopPerformingStoresAsync(PerformanceCriteria criteria = PerformanceCriteria.Revenue, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a store can be created for a seller
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <param name="storeName">Proposed store name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stores by category with pagination
        /// </summary>
        /// <param name="category">Store category</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stores in the specified category</returns>
        Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates store status (active/inactive)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="isActive">New active status</param>
        /// <param name="reason">Reason for status change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store response</returns>
        Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, bool isActive, string? reason = null, CancellationToken cancellationToken = default);
    }

}
