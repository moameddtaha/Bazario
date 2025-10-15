using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums.Store;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StoreAnalyticsService> _logger;

        public StoreAnalyticsService(
            IUnitOfWork unitOfWork,
            ILogger<StoreAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreAnalytics> GetStoreAnalyticsAsync(Guid storeId, DateRange? dateRange = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting analytics for store: {StoreId}", storeId);

            try
            {
                // Validate store exists
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Set default date range if not provided
                dateRange ??= new DateRange();

                // Get aggregated data efficiently from database
                var orderStats = await _unitOfWork.Orders.GetStoreOrderStatsAsync(storeId, dateRange.StartDate, dateRange.EndDate, cancellationToken);
                var reviewStats = await _unitOfWork.Reviews.GetStoreReviewStatsAsync(storeId, cancellationToken);
                var topProducts = await _unitOfWork.Products.GetTopPerformingProductsAsync(storeId, 10, cancellationToken);

                // Get product counts efficiently (no need to load all products)
                var totalProductCount = await _unitOfWork.Products.GetProductCountByStoreIdAsync(storeId, includeDeleted: true, cancellationToken);
                var activeProductCount = await _unitOfWork.Products.GetProductCountByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);

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
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Get performance data efficiently from database
                // Use last 12 months for performance metrics (more reasonable than all-time data)
                var oneYearAgo = DateTime.UtcNow.AddYears(-1);
                var orderStats = await _unitOfWork.Orders.GetStoreOrderStatsAsync(storeId, oneYearAgo, DateTime.UtcNow, cancellationToken);
                var reviewStats = await _unitOfWork.Reviews.GetStoreReviewStatsAsync(storeId, cancellationToken);
                var productCount = await _unitOfWork.Stores.GetProductCountByStoreIdAsync(storeId, cancellationToken);

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
                // Start with base queryable
                var query = _unitOfWork.Stores.GetStoresQueryable();

                // Apply soft deletion filters (consistent with SearchStoresAsync)
                if (searchCriteria.OnlyDeleted)
                {
                    query = _unitOfWork.Stores.GetStoresQueryableIgnoreFilters().Where(s => s.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    query = _unitOfWork.Stores.GetStoresQueryableIgnoreFilters();
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

                // Get total count for pagination
                var totalCount = await _unitOfWork.Stores.GetStoresCountAsync(query, cancellationToken);

                // Convert performance criteria to string for repository method
                var performanceCriteriaString = performanceCriteria switch
                {
                    PerformanceCriteria.Revenue => "revenue",
                    PerformanceCriteria.Orders => "orders",
                    PerformanceCriteria.Rating => "rating",
                    PerformanceCriteria.CustomerCount => "customers",
                    PerformanceCriteria.ProductCount => "products",
                    _ => "revenue"
                };

                // Get top performing stores with performance metrics calculated and sorted at database level
                // Use last 12 months for performance metrics (configurable)
                var performancePeriodStart = DateTime.UtcNow.AddMonths(-12);
                var storePerformances = await _unitOfWork.Stores.GetTopPerformingStoresAsync(
                    query, 
                    searchCriteria.PageNumber, 
                    searchCriteria.PageSize, 
                    performanceCriteriaString, 
                    performancePeriodStart,
                    cancellationToken);

                var result = new PagedResponse<StorePerformance>
                {
                    Items = storePerformances,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully retrieved {CalculatedCount} top performing stores out of {TotalCount} total stores. Category: {Category}, SellerId: {SellerId}, Criteria: {PerformanceCriteria}", 
                    storePerformances.Count, totalCount, searchCriteria.Category ?? "All", searchCriteria.SellerId, performanceCriteria);
                
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
