using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO;
using Bazario.Core.Extensions;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services
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
                searchCriteria?.SearchTerm, searchCriteria?.Category);

            try
            {
                if (searchCriteria == null)
                {
                    searchCriteria = new StoreSearchCriteria();
                }

                // Build filter predicate
                var allStores = await _storeRepository.GetAllStoresAsync(cancellationToken);
                var filteredStores = allStores.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
                {
                    filteredStores = filteredStores.Where(s => 
                        (s.Name != null && s.Name.Contains(searchCriteria.SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (s.Description != null && s.Description.Contains(searchCriteria.SearchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrWhiteSpace(searchCriteria.Category))
                {
                    filteredStores = filteredStores.Where(s => 
                        string.Equals(s.Category, searchCriteria.Category, StringComparison.OrdinalIgnoreCase));
                }

                if (searchCriteria.SellerId.HasValue)
                {
                    filteredStores = filteredStores.Where(s => s.SellerId == searchCriteria.SellerId.Value);
                }

                // Apply sorting
                filteredStores = searchCriteria.SortBy?.ToLower() switch
                {
                    "name" => searchCriteria.SortDescending ? 
                        filteredStores.OrderByDescending(s => s.Name) : 
                        filteredStores.OrderBy(s => s.Name),
                    "createdat" => searchCriteria.SortDescending ? 
                        filteredStores.OrderByDescending(s => s.CreatedAt) : 
                        filteredStores.OrderBy(s => s.CreatedAt),
                    _ => filteredStores.OrderBy(s => s.Name)
                };

                // Get total count
                var totalCount = filteredStores.Count();

                // Apply pagination
                var pagedStores = filteredStores
                    .Skip((searchCriteria.PageNumber - 1) * searchCriteria.PageSize)
                    .Take(searchCriteria.PageSize)
                    .Select(s => s.ToStoreResponse())
                    .ToList();

                var result = new PagedResponse<StoreResponse>
                {
                    Items = pagedStores,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully searched stores. Found {TotalCount} stores, returning page {PageNumber} with {ItemCount} items", 
                    totalCount, searchCriteria.PageNumber, pagedStores.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search stores");
                throw;
            }
        }

        public async Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting stores by category: {Category}, Page: {PageNumber}, Size: {PageSize}", 
                category, pageNumber, pageSize);

            try
            {
                var stores = await _storeRepository.GetStoresByCategoryAsync(category, cancellationToken);
                var totalCount = stores.Count;

                var pagedStores = stores
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => s.ToStoreResponse())
                    .ToList();

                var result = new PagedResponse<StoreResponse>
                {
                    Items = pagedStores,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogDebug("Successfully retrieved stores by category: {Category}. Found {TotalCount} stores", 
                    category, totalCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stores by category: {Category}", category);
                throw;
            }
        }
    }
}
