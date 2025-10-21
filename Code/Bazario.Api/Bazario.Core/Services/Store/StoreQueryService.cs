using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Store;
using Bazario.Core.Extensions.Store;
using Bazario.Core.Helpers.Store;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store query operations
    /// Handles store retrieval, search, and filtering
    /// </summary>
    public class StoreQueryService : IStoreQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreQueryHelper _queryHelper;
        private readonly ILogger<StoreQueryService> _logger;

        public StoreQueryService(
            IUnitOfWork unitOfWork,
            IStoreQueryHelper queryHelper,
            ILogger<StoreQueryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _queryHelper = queryHelper ?? throw new ArgumentNullException(nameof(queryHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreResponse?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving store by ID: {StoreId}", storeId);

            try
            {
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);

                if (store == null)
                {
                    _logger.LogDebug("Store not found. StoreId: {StoreId}", storeId);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved store. StoreId: {StoreId}, Name: {StoreName}", store.StoreId, store.Name);
                return store.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve store by ID: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<List<StoreResponse>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving stores for seller: {SellerId}", sellerId);

            try
            {
                if (sellerId == Guid.Empty)
                {
                    throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
                }

                var stores = await _unitOfWork.Stores.GetStoresBySellerIdAsync(sellerId, cancellationToken);
                var storeResponses = stores.Select(s => s.ToStoreResponse()).ToList();

                _logger.LogDebug("Successfully retrieved {StoreCount} stores for seller: {SellerId}", storeResponses.Count, sellerId);
                return storeResponses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve stores for seller: {SellerId}", sellerId);
                throw;
            }
        }

        public async Task<PagedResponse<StoreResponse>> SearchStoresAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(searchCriteria);

            _logger.LogDebug("Searching stores with criteria: {SearchTerm}, Category: {Category}",
                searchCriteria.SearchTerm, searchCriteria.Category);

            try
            {
                // Validate that at least one search criterion is provided
                if (string.IsNullOrWhiteSpace(searchCriteria.SearchTerm) &&
                    string.IsNullOrWhiteSpace(searchCriteria.Category) &&
                    !searchCriteria.SellerId.HasValue)
                {
                    throw new ArgumentException("At least one search criterion (SearchTerm, Category, or SellerId) must be provided", nameof(searchCriteria));
                }

                // Start with IQueryable - stays as SQL
                var query = _unitOfWork.Stores.GetStoresQueryable();

                // Apply soft deletion filters using helper method
                query = _queryHelper.ApplySoftDeletionFilters(query, searchCriteria);

                // Apply search filter using repository method (case-insensitive)
                if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
                {
                    query = _unitOfWork.Stores.ApplySearchFilter(query, searchCriteria.SearchTerm);
                }

                // Apply category filter using repository method (case-insensitive)
                if (!string.IsNullOrWhiteSpace(searchCriteria.Category))
                {
                    query = _unitOfWork.Stores.ApplyCategoryFilter(query, searchCriteria.Category);
                }

                // Apply seller filter
                if (searchCriteria.SellerId.HasValue)
                {
                    if (searchCriteria.SellerId.Value == Guid.Empty)
                    {
                        throw new ArgumentException("Seller ID cannot be empty", nameof(searchCriteria));
                    }
                    query = query.Where(s => s.SellerId == searchCriteria.SellerId.Value);
                }

                // Apply sorting using helper method
                query = _queryHelper.ApplySorting(query, searchCriteria);

                // Get total count with SQL COUNT
                var totalCount = await _unitOfWork.Stores.GetStoresCountAsync(query, cancellationToken);

                // Apply pagination and execute query (this becomes SQL OFFSET/FETCH)
                var stores = await _unitOfWork.Stores.GetStoresPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                var storeResponses = stores.Select(s => s.ToStoreResponse()).ToList();

                var result = new PagedResponse<StoreResponse>
                {
                    Items = storeResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully searched stores. Found {TotalCount} stores, returning page {PageNumber} with {ItemCount} items",
                    totalCount, searchCriteria.PageNumber, storeResponses.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search stores");
                throw;
            }
        }

        public async Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(searchCriteria);

            _logger.LogDebug("Getting stores by category: {Category}, Page: {PageNumber}, Size: {PageSize}",
                searchCriteria.Category, searchCriteria.PageNumber, searchCriteria.PageSize);

            try
            {
                // Validate that category is provided
                if (string.IsNullOrWhiteSpace(searchCriteria.Category))
                {
                    throw new ArgumentException("Category is required for GetStoresByCategoryAsync", nameof(searchCriteria));
                }

                // Start with IQueryable
                var query = _unitOfWork.Stores.GetStoresQueryable();

                // Apply soft deletion filters using helper method
                query = _queryHelper.ApplySoftDeletionFilters(query, searchCriteria);

                // Apply category filter using repository method (case-insensitive)
                query = _unitOfWork.Stores.ApplyCategoryFilter(query, searchCriteria.Category);

                // Apply sorting using helper method
                query = _queryHelper.ApplySorting(query, searchCriteria);

                // Get total count with SQL COUNT
                var totalCount = await _unitOfWork.Stores.GetStoresCountAsync(query, cancellationToken);

                // Apply pagination and execute query
                var stores = await _unitOfWork.Stores.GetStoresPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                var storeResponses = stores.Select(s => s.ToStoreResponse()).ToList();

                var result = new PagedResponse<StoreResponse>
                {
                    Items = storeResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully retrieved stores by category: {Category}. Found {TotalCount} stores",
                    searchCriteria.Category, totalCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stores by category: {Category}", searchCriteria.Category);
                throw;
            }
        }
    }
}
