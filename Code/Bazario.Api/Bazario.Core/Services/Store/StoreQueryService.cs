using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Store;
using Bazario.Core.Extensions;
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
        private readonly IStoreRepository _storeRepository;
        private readonly ILogger<StoreQueryService> _logger;

        public StoreQueryService(
            IStoreRepository storeRepository,
            ILogger<StoreQueryService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreResponse?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving store by ID: {StoreId}", storeId);

            try
            {
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                
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
                var stores = await _storeRepository.GetStoresBySellerIdAsync(sellerId, cancellationToken);
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
                var query = _storeRepository.GetStoresQueryable();

                // Apply soft deletion filters (these become SQL WHERE clauses)
                if (searchCriteria.OnlyDeleted)
                {
                    // Need to ignore the global filter to get deleted stores
                    query = _storeRepository.GetStoresQueryableIgnoreFilters().Where(s => s.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    // Need to ignore the global filter to include both active and deleted stores
                    query = _storeRepository.GetStoresQueryableIgnoreFilters();
                }
                // If neither OnlyDeleted nor IncludeDeleted, the global HasQueryFilter 
                // automatically applies !s.IsDeleted, so no additional filter needed

                // Apply filters (these become SQL WHERE clauses)
                if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
                {
                    query = query.Where(s => 
                        s.Name != null && s.Name.Contains(searchCriteria.SearchTerm) ||
                        s.Description != null && s.Description.Contains(searchCriteria.SearchTerm));
                }

                if (!string.IsNullOrWhiteSpace(searchCriteria.Category))
                {
                    query = query.Where(s => 
                        string.Equals(s.Category, searchCriteria.Category, StringComparison.OrdinalIgnoreCase));
                }

                if (searchCriteria.SellerId.HasValue)
                {
                    query = query.Where(s => s.SellerId == searchCriteria.SellerId.Value);
                }

                // Apply sorting (this becomes SQL ORDER BY)
                query = searchCriteria.SortBy?.ToLower() switch
                {
                    "name" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(s => s.Name) : 
                        query.OrderBy(s => s.Name),
                    "createdat" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(s => s.CreatedAt) : 
                        query.OrderBy(s => s.CreatedAt),
                    _ => query.OrderBy(s => s.Name)
                };

                // Get total count with SQL COUNT
                var totalCount = await _storeRepository.GetStoresCountAsync(query, cancellationToken);

                // Apply pagination and execute query (this becomes SQL OFFSET/FETCH)
                var stores = await _storeRepository.GetStoresPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

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
            _logger.LogDebug("Getting stores by category: {Category}, Page: {PageNumber}, Size: {PageSize}", 
                searchCriteria.Category, searchCriteria.PageNumber, searchCriteria.PageSize);

            try
            {
                // Validate that category is provided
                if (string.IsNullOrWhiteSpace(searchCriteria.Category))
                {
                    throw new ArgumentException("Category is required for GetStoresByCategoryAsync", nameof(searchCriteria));
                }

                // Use IQueryable for efficient SQL
                var query = _storeRepository.GetStoresQueryable()
                    .Where(s => s.Category == searchCriteria.Category);

                // Apply soft deletion filters (consistent with SearchStoresAsync)
                if (searchCriteria.OnlyDeleted)
                {
                    query = _storeRepository.GetStoresQueryableIgnoreFilters()
                        .Where(s => s.Category == searchCriteria.Category)
                        .Where(s => s.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    query = _storeRepository.GetStoresQueryableIgnoreFilters()
                        .Where(s => s.Category == searchCriteria.Category);
                }

                // Apply sorting (consistent with SearchStoresAsync)
                query = searchCriteria.SortBy?.ToLower() switch
                {
                    "name" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(s => s.Name) : 
                        query.OrderBy(s => s.Name),
                    "createdat" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(s => s.CreatedAt) : 
                        query.OrderBy(s => s.CreatedAt),
                    _ => query.OrderBy(s => s.Name)
                };

                // Get total count with SQL COUNT
                var totalCount = await _storeRepository.GetStoresCountAsync(query, cancellationToken);

                // Apply pagination and execute query
                var stores = await _storeRepository.GetStoresPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

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
