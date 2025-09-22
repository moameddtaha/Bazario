using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store analytics and performance reporting
    /// Handles store analytics, performance metrics, and reporting
    /// </summary>
    public class StoreAnalyticsService : IStoreAnalyticsService
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IProductRepository _productRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<StoreAnalyticsService> _logger;

        public StoreAnalyticsService(
            IStoreRepository storeRepository,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IProductRepository productRepository,
            IReviewRepository reviewRepository,
            ILogger<StoreAnalyticsService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _orderItemRepository = orderItemRepository ?? throw new ArgumentNullException(nameof(orderItemRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreAnalytics> GetStoreAnalyticsAsync(Guid storeId, DateRange? dateRange = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting analytics for store: {StoreId}", storeId);

            try
            {
                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Set default date range if not provided
                dateRange ??= new DateRange();

                // Get aggregated data efficiently from database
                var orderStats = await _orderRepository.GetStoreOrderStatsAsync(storeId, dateRange.StartDate, dateRange.EndDate, cancellationToken);
                var reviewStats = await _reviewRepository.GetStoreReviewStatsAsync(storeId, cancellationToken);
                var topProducts = await _productRepository.GetTopPerformingProductsAsync(storeId, 10, cancellationToken);

                // Get product counts efficiently (no need to load all products)
                var totalProductCount = await _productRepository.GetProductCountByStoreIdAsync(storeId, includeDeleted: true, cancellationToken);
                var activeProductCount = await _productRepository.GetProductCountByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);

                // Convert monthly order data to monthly store data
                var monthlyData = orderStats.MonthlyData.Select(m => new MonthlyStoreData
                {
                    Year = m.Year,
                    Month = m.Month,
                    Orders = m.Orders,
                    Revenue = m.Revenue,
                    NewCustomers = m.NewCustomers,
                    ProductsSold = m.ProductsSold
                }).ToList();

                var analytics = new StoreAnalytics
                {
                    StoreId = storeId,
                    StoreName = store.Name,
                    AnalyticsPeriod = dateRange,
                    TotalProducts = totalProductCount,
                    ActiveProducts = activeProductCount,
                    TotalOrders = orderStats.TotalOrders,
                    TotalRevenue = orderStats.TotalRevenue,
                    AverageOrderValue = orderStats.AverageOrderValue,
                    TotalCustomers = orderStats.TotalCustomers,
                    RepeatCustomers = orderStats.RepeatCustomers,
                    CustomerRetentionRate = orderStats.CustomerRetentionRate,
                    AverageRating = reviewStats.AverageRating,
                    TotalReviews = reviewStats.TotalReviews,
                    TopProducts = topProducts,
                    MonthlyData = monthlyData
                };

                _logger.LogDebug("Successfully retrieved analytics for store: {StoreId}", storeId);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get analytics for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<StorePerformance> GetStorePerformanceAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting performance data for store: {StoreId}", storeId);

            try
            {
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Get performance data efficiently from database
                // Use last 12 months for performance metrics (more reasonable than all-time data)
                var oneYearAgo = DateTime.UtcNow.AddYears(-1);
                var orderStats = await _orderRepository.GetStoreOrderStatsAsync(storeId, oneYearAgo, DateTime.UtcNow, cancellationToken);
                var reviewStats = await _reviewRepository.GetStoreReviewStatsAsync(storeId, cancellationToken);
                var productCount = await _storeRepository.GetProductCountByStoreIdAsync(storeId, cancellationToken);

                // Log warning if CreatedAt is null (data integrity issue)
                if (store.CreatedAt == null)
                {
                    _logger.LogWarning("Store {StoreId} has null CreatedAt - this suggests a data integrity issue", storeId);
                }

                var performance = new StorePerformance
                {
                    StoreId = storeId,
                    StoreName = store.Name,
                    Category = store.Category,
                    CreatedAt = store.CreatedAt ?? DateTime.UtcNow, // Fallback for data integrity issues
                    IsActive = store.IsActive,
                    ProductCount = productCount,
                    TotalRevenue = orderStats.TotalRevenue,
                    TotalOrders = orderStats.TotalOrders,
                    AverageRating = reviewStats.AverageRating,
                    ReviewCount = reviewStats.TotalReviews,
                    Rank = null // Nullable - will be set by GetTopPerformingStoresAsync when needed
                };

                _logger.LogDebug("Successfully retrieved performance data for store: {StoreId}", storeId);
                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get performance data for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<PagedResponse<StorePerformance>> GetTopPerformingStoresAsync(PerformanceCriteria performanceCriteria = PerformanceCriteria.Revenue, StoreSearchCriteria? searchCriteria = null, CancellationToken cancellationToken = default)
        {
            // Set default search criteria if not provided
            searchCriteria ??= new StoreSearchCriteria { PageNumber = 1, PageSize = 10 };

            _logger.LogDebug("Getting top performing stores with criteria: {PerformanceCriteria}, Category: {Category}, SellerId: {SellerId}, Page: {PageNumber}, Size: {PageSize}", 
                performanceCriteria, searchCriteria.Category ?? "All", searchCriteria.SellerId, searchCriteria.PageNumber, searchCriteria.PageSize);

            try
            {
                // ✅ MAJOR PERFORMANCE IMPROVEMENT: Only calculate performance for the requested page
                // ✅ BETTER APPROACH: Use existing StoreSearchCriteria for consistent filtering
                
                // Start with base queryable
                var query = _storeRepository.GetStoresQueryable();

                // Apply soft deletion filters (consistent with SearchStoresAsync)
                if (searchCriteria.OnlyDeleted)
                {
                    query = _storeRepository.GetStoresQueryableIgnoreFilters().Where(s => s.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    query = _storeRepository.GetStoresQueryableIgnoreFilters();
                }
                // Default: only active stores (global HasQueryFilter applies)

                // Apply filters (these become SQL WHERE clauses) - reusing logic from SearchStoresAsync
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
                    _logger.LogDebug("Filtering stores by category: {Category}", searchCriteria.Category);
                }

                if (searchCriteria.SellerId.HasValue)
                {
                    query = query.Where(s => s.SellerId == searchCriteria.SellerId.Value);
                    _logger.LogDebug("Filtering stores by seller: {SellerId}", searchCriteria.SellerId);
                }

                if (searchCriteria.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == searchCriteria.IsActive.Value);
                }

                // ✅ IMPROVED SORTING: Sort by meaningful criteria at database level where possible
                var sortedQuery = performanceCriteria switch
                {
                    PerformanceCriteria.ProductCount => query.OrderByDescending(s => s.Products != null ? s.Products.Count() : 0),
                    // For complex criteria, use StoreSearchCriteria sorting or default to creation date
                    _ => searchCriteria.SortBy?.ToLower() switch
                    {
                        "name" => searchCriteria.SortDescending ? 
                            query.OrderByDescending(s => s.Name) : 
                            query.OrderBy(s => s.Name),
                        "createdat" => searchCriteria.SortDescending ? 
                            query.OrderByDescending(s => s.CreatedAt) : 
                            query.OrderBy(s => s.CreatedAt),
                        _ => query.OrderBy(s => s.Category).ThenByDescending(s => s.CreatedAt)
                    }
                };

                // Get total count and basic store info for the requested page
                var totalCount = await _storeRepository.GetStoresCountAsync(query, cancellationToken);
                var pagedStores = await _storeRepository.GetStoresPagedAsync(sortedQuery, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                _logger.LogDebug("Retrieved {PagedStoreCount} stores for performance calculation out of {TotalCount} total stores", 
                    pagedStores.Count, totalCount);

                // ✅ PERFORMANCE FIX: Only calculate detailed performance for the paged stores (e.g., 10 stores instead of 1000)
                var storePerformances = new List<StorePerformance>();
                
                for (int i = 0; i < pagedStores.Count; i++)
                {
                    var store = pagedStores[i];
                    var performance = await GetStorePerformanceAsync(store.StoreId, cancellationToken);
                    performance.Rank = (searchCriteria.PageNumber - 1) * searchCriteria.PageSize + i + 1;
                    storePerformances.Add(performance);
                }

                // If sorting by complex criteria, we need to re-sort the performance results
                if (performanceCriteria != PerformanceCriteria.ProductCount)
                {
                    storePerformances = performanceCriteria switch
                    {
                        PerformanceCriteria.Revenue => storePerformances.OrderByDescending(s => s.TotalRevenue).ToList(),
                        PerformanceCriteria.Orders => storePerformances.OrderByDescending(s => s.TotalOrders).ToList(),
                        PerformanceCriteria.Rating => storePerformances.OrderByDescending(s => s.AverageRating).ToList(),
                        PerformanceCriteria.CustomerCount => storePerformances.OrderByDescending(s => s.ReviewCount).ToList(),
                        _ => storePerformances.OrderByDescending(s => s.TotalRevenue).ToList()
                    };

                    // Reassign rankings after sorting
                    for (int i = 0; i < storePerformances.Count; i++)
                    {
                        storePerformances[i].Rank = (searchCriteria.PageNumber - 1) * searchCriteria.PageSize + i + 1;
                    }
                }

                var result = new PagedResponse<StorePerformance>
                {
                    Items = storePerformances,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully calculated performance for {CalculatedCount} stores out of {TotalCount} total stores. Category: {Category}, SellerId: {SellerId}", 
                    storePerformances.Count, totalCount, searchCriteria.Category ?? "All", searchCriteria.SellerId);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get top performing stores");
                throw;
            }
        }
    }
}
