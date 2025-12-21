using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Models.Catalog.Discount;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Service for discount analytics and performance tracking.
    /// Provides comprehensive metrics including usage statistics, performance analysis,
    /// revenue impact, and trend analysis for discount campaigns.
    /// </summary>
    /// <remarks>
    /// Note: This service has known N+1 query issues in bulk methods (GetAllDiscountUsageStatsAsync,
    /// GetAllDiscountPerformanceAsync, GetStoreDiscountUsageStatsAsync). For production use with
    /// large discount counts, consider implementing bulk repository methods with database-level aggregation.
    /// </remarks>
    public class DiscountAnalyticsService : IDiscountAnalyticsService
    {
        private readonly IDiscountRepository _discountRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<DiscountAnalyticsService> _logger;

        public DiscountAnalyticsService(
            IDiscountRepository discountRepository,
            IOrderRepository orderRepository,
            ILogger<DiscountAnalyticsService> logger)
        {
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets comprehensive usage statistics for a specific discount code.
        /// </summary>
        /// <param name="discountCode">The discount code to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Usage statistics including order count, revenue, and average values.
        /// Returns null if discount code not found.
        /// Returns empty stats if discount exists but has never been used.
        /// </returns>
        public async Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting usage stats for discount code: {Code}", discountCode);

            var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode, cancellationToken);
            if (discount == null)
            {
                _logger.LogWarning("Discount not found: {Code}", discountCode);
                return null;
            }

            // Get all orders with this discount code to calculate stats
            // Removed redundant orderCount query - we can check orders.Count directly
            var orders = await _orderRepository.GetOrdersByDiscountCodeAsync(discountCode, cancellationToken);

            // Null safety check
            if (orders == null || orders.Count == 0)
            {
                return CreateEmptyUsageStats(discountCode, discount);
            }

            var stats = new DiscountUsageStats
            {
                DiscountCode = discountCode,
                UsageCount = orders.Count,
                TotalDiscountAmount = orders.Sum(o => o.DiscountAmount),
                AverageDiscountAmount = orders.Average(o => o.DiscountAmount),
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Average(o => o.TotalAmount),
                FirstUsed = orders.Min(o => o.Date),
                LastUsed = orders.Max(o => o.Date),
                IsActive = discount.IsActive,
                StoreId = discount.ApplicableStoreId,
                StoreName = discount.Store?.Name
            };

            return stats;
        }

        /// <summary>
        /// Gets usage statistics for all valid discounts within a date range.
        /// Uses bulk database aggregation to avoid N+1 query problems.
        /// </summary>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// List of usage statistics ordered by total revenue (descending).
        /// </returns>
        public async Task<List<DiscountUsageStats>> GetAllDiscountUsageStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting usage stats for all discounts");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var discounts = await _discountRepository.GetValidDiscountsAsync(startDate.Value, endDate.Value, cancellationToken);

            if (discounts == null || discounts.Count == 0)
            {
                return new List<DiscountUsageStats>();
            }

            // PERFORMANCE FIX: Use bulk repository method instead of N+1 queries
            // Single query to get all order statistics grouped by discount code
            var discountCodes = discounts.Select(d => d.Code).ToList();
            var orderStats = await _orderRepository.GetOrderStatsByDiscountCodesAsync(discountCodes, cancellationToken);

            // Build stats in memory (no additional database queries)
            var statsList = new List<DiscountUsageStats>();
            foreach (var discount in discounts)
            {
                var orderStat = orderStats.FirstOrDefault(s => s.DiscountCode.Equals(discount.Code, StringComparison.OrdinalIgnoreCase));

                var stats = orderStat != null && orderStat.OrderCount > 0
                    ? new DiscountUsageStats
                    {
                        DiscountCode = discount.Code,
                        UsageCount = orderStat.OrderCount,
                        TotalDiscountAmount = orderStat.TotalDiscountAmount,
                        AverageDiscountAmount = orderStat.AverageDiscountAmount,
                        TotalRevenue = orderStat.TotalRevenue,
                        AverageOrderValue = orderStat.AverageOrderValue,
                        FirstUsed = orderStat.FirstUsed,
                        LastUsed = orderStat.LastUsed,
                        IsActive = discount.IsActive,
                        StoreId = discount.ApplicableStoreId,
                        StoreName = discount.Store?.Name
                    }
                    : CreateEmptyUsageStats(discount.Code, discount);

                statsList.Add(stats);
            }

            return statsList.OrderByDescending(s => s.TotalRevenue).ToList();
        }

        /// <summary>
        /// Gets detailed performance metrics for a specific discount code within a date range.
        /// </summary>
        /// <param name="discountCode">The discount code to analyze</param>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Performance metrics including conversion rate, revenue, and averages.
        /// Returns null if discount code not found.
        /// Returns empty performance if no orders in the specified date range.
        /// </returns>
        public async Task<DiscountPerformance?> GetDiscountPerformanceAsync(
            string discountCode,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting performance metrics for discount code: {Code}", discountCode);

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode, cancellationToken);
            if (discount == null)
            {
                return null;
            }

            var discountOrders = await _orderRepository.GetOrdersByDiscountCodeAndDateRangeAsync(
                discountCode, startDate.Value, endDate.Value, cancellationToken);

            // Null safety check
            if (discountOrders == null || discountOrders.Count == 0)
            {
                return CreateEmptyPerformance(discount, startDate.Value, endDate.Value);
            }

            var totalOrderCount = await _orderRepository.GetOrdersCountByDateRangeAsync(
                startDate.Value, endDate.Value, cancellationToken);

            var performance = new DiscountPerformance
            {
                DiscountCode = discountCode,
                DiscountType = discount.Type,
                DiscountValue = discount.Value,
                OrderCount = discountOrders.Count,
                TotalRevenue = discountOrders.Sum(o => o.TotalAmount),
                TotalDiscountGiven = discountOrders.Sum(o => o.DiscountAmount),
                NetRevenue = discountOrders.Sum(o => o.TotalAmount - o.DiscountAmount),
                ConversionRate = totalOrderCount > 0 ? (decimal)discountOrders.Count / totalOrderCount * 100 : 0,
                AverageOrderValue = discountOrders.Average(o => o.TotalAmount),
                AverageDiscountPerOrder = discountOrders.Average(o => o.DiscountAmount),
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                StoreId = discount.ApplicableStoreId,
                StoreName = discount.Store?.Name
            };

            return performance;
        }

        /// <summary>
        /// Gets performance metrics for all valid discounts within a date range.
        /// PERFORMANCE OPTIMIZED: Uses bulk repository method to avoid N+1 query problems.
        /// </summary>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// List of performance metrics ordered by total revenue (descending).
        /// Only includes discounts with at least one order in the date range.
        /// </returns>
        public async Task<List<DiscountPerformance>> GetAllDiscountPerformanceAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting performance metrics for all discounts");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var discounts = await _discountRepository.GetValidDiscountsAsync(startDate.Value, endDate.Value, cancellationToken);

            // PERFORMANCE FIX: Use bulk repository method instead of N+1 queries
            // Single query to get all order statistics grouped by discount code with date filtering
            var discountCodes = discounts.Select(d => d.Code).ToList();
            var orderStats = await _orderRepository.GetOrderStatsByDiscountCodesAndDateRangeAsync(
                discountCodes, startDate.Value, endDate.Value, cancellationToken);

            // Get total order count for conversion rate calculation (single query)
            var totalOrderCount = await _orderRepository.GetOrdersCountByDateRangeAsync(
                startDate.Value, endDate.Value, cancellationToken);

            // Build performance list in memory (no additional database queries)
            var performanceList = new List<DiscountPerformance>();
            foreach (var discount in discounts)
            {
                var orderStat = orderStats.FirstOrDefault(s => s.DiscountCode.Equals(discount.Code, StringComparison.OrdinalIgnoreCase));

                // Only include discounts that have orders in the date range
                if (orderStat != null && orderStat.OrderCount > 0)
                {
                    var performance = new DiscountPerformance
                    {
                        DiscountCode = discount.Code,
                        DiscountType = discount.Type,
                        DiscountValue = discount.Value,
                        OrderCount = orderStat.OrderCount,
                        TotalRevenue = orderStat.TotalRevenue,
                        TotalDiscountGiven = orderStat.TotalDiscountAmount,
                        NetRevenue = orderStat.TotalRevenue - orderStat.TotalDiscountAmount,
                        ConversionRate = totalOrderCount > 0 ? (decimal)orderStat.OrderCount / totalOrderCount * 100 : 0,
                        AverageOrderValue = orderStat.AverageOrderValue,
                        AverageDiscountPerOrder = orderStat.AverageDiscountAmount,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value,
                        StoreId = discount.ApplicableStoreId,
                        StoreName = discount.Store?.Name
                    };

                    performanceList.Add(performance);
                }
            }

            return performanceList.OrderByDescending(p => p.TotalRevenue).ToList();
        }

        /// <summary>
        /// Analyzes the overall revenue impact of discount usage across all orders.
        /// Compares discounted vs non-discounted orders to measure discount effectiveness.
        /// </summary>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Revenue impact analysis including total revenue, discount costs, usage rates, and comparative metrics.
        /// </returns>
        public async Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount revenue impact analysis");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var allOrders = await _orderRepository.GetOrdersByDateRangeAsync(startDate.Value, endDate.Value, cancellationToken);

            // Null safety check
            allOrders ??= [];

            var discountedOrders = allOrders.Where(o => o.DiscountAmount > 0).ToList();
            var nonDiscountedOrders = allOrders.Where(o => o.DiscountAmount == 0).ToList();

            var impact = new DiscountRevenueImpact
            {
                TotalRevenue = allOrders.Sum(o => o.TotalAmount),
                DiscountedOrderRevenue = discountedOrders.Sum(o => o.TotalAmount),
                NonDiscountedOrderRevenue = nonDiscountedOrders.Sum(o => o.TotalAmount),
                TotalDiscountsGiven = discountedOrders.Sum(o => o.DiscountAmount),
                NetRevenue = allOrders.Sum(o => o.TotalAmount - o.DiscountAmount),
                DiscountedOrderCount = discountedOrders.Count,
                NonDiscountedOrderCount = nonDiscountedOrders.Count,
                TotalOrderCount = allOrders.Count,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            // Calculate percentages
            if (impact.TotalRevenue > 0)
            {
                impact.DiscountedRevenuePercentage = (impact.DiscountedOrderRevenue / impact.TotalRevenue) * 100;
            }

            if (impact.DiscountedOrderCount > 0)
            {
                impact.AverageDiscountedOrderValue = impact.DiscountedOrderRevenue / impact.DiscountedOrderCount;
            }

            if (impact.NonDiscountedOrderCount > 0)
            {
                impact.AverageNonDiscountedOrderValue = impact.NonDiscountedOrderRevenue / impact.NonDiscountedOrderCount;
            }

            if (impact.TotalOrderCount > 0)
            {
                impact.DiscountUsageRate = ((decimal)impact.DiscountedOrderCount / impact.TotalOrderCount) * 100;
            }

            return impact;
        }

        /// <summary>
        /// Gets the top performing discounts ranked by total revenue.
        /// </summary>
        /// <param name="topCount">Number of top discounts to return (default: 10)</param>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// List of top performing discounts ordered by total revenue (descending).
        /// Limited to topCount results.
        /// </returns>
        public async Task<List<DiscountPerformance>> GetTopPerformingDiscountsAsync(
            int topCount = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting top {Count} performing discounts", topCount);

            var allPerformance = await GetAllDiscountPerformanceAsync(startDate, endDate, cancellationToken);

            return allPerformance
                .OrderByDescending(p => p.TotalRevenue)
                .Take(topCount)
                .ToList();
        }

        /// <summary>
        /// Gets usage statistics for all discounts associated with a specific store.
        /// PERFORMANCE OPTIMIZED: Uses bulk repository method to avoid N+1 query problems.
        /// </summary>
        /// <param name="storeId">The ID of the store to analyze</param>
        /// <param name="startDate">Start date for analysis (defaults to 12 months ago)</param>
        /// <param name="endDate">End date for analysis (defaults to current date)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// List of usage statistics for store discounts ordered by total revenue (descending).
        /// </returns>
        public async Task<List<DiscountUsageStats>> GetStoreDiscountUsageStatsAsync(
            Guid storeId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount usage stats for store: {StoreId}", storeId);

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var storeDiscounts = await _discountRepository.GetDiscountsByStoreIdAsync(storeId, cancellationToken);

            // PERFORMANCE FIX: Use bulk repository method instead of N+1 queries
            // Single query to get all order statistics grouped by discount code with date filtering
            var discountCodes = storeDiscounts.Select(d => d.Code).ToList();
            var orderStats = await _orderRepository.GetOrderStatsByDiscountCodesAndDateRangeAsync(
                discountCodes, startDate.Value, endDate.Value, cancellationToken);

            // Build stats list in memory (no additional database queries)
            var statsList = new List<DiscountUsageStats>();
            foreach (var discount in storeDiscounts)
            {
                var orderStat = orderStats.FirstOrDefault(s => s.DiscountCode.Equals(discount.Code, StringComparison.OrdinalIgnoreCase));

                var stats = orderStat != null && orderStat.OrderCount > 0
                    ? new DiscountUsageStats
                    {
                        DiscountCode = discount.Code,
                        UsageCount = orderStat.OrderCount,
                        TotalDiscountAmount = orderStat.TotalDiscountAmount,
                        AverageDiscountAmount = orderStat.AverageDiscountAmount,
                        TotalRevenue = orderStat.TotalRevenue,
                        AverageOrderValue = orderStat.AverageOrderValue,
                        FirstUsed = orderStat.FirstUsed,
                        LastUsed = orderStat.LastUsed,
                        IsActive = discount.IsActive,
                        StoreId = discount.ApplicableStoreId,
                        StoreName = discount.Store?.Name
                    }
                    : CreateEmptyUsageStats(discount.Code, discount);

                statsList.Add(stats);
            }

            return statsList.OrderByDescending(s => s.TotalRevenue).ToList();
        }

        /// <summary>
        /// Gets high-level overview statistics for the entire discount system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Tuple containing: (TotalCreated - all discounts, TotalUsed - at least one use, TotalActive - currently active).
        /// </returns>
        public async Task<(int TotalCreated, int TotalUsed, int TotalActive)> GetOverallDiscountStatsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting overall discount statistics");
            return await _discountRepository.GetDiscountUsageStatsAsync(cancellationToken);
        }

        // Helper methods
        private DiscountUsageStats CreateEmptyUsageStats(string discountCode, Domain.Entities.Catalog.Discount discount)
        {
            return new DiscountUsageStats
            {
                DiscountCode = discountCode,
                UsageCount = 0,
                TotalDiscountAmount = 0,
                AverageDiscountAmount = 0,
                TotalRevenue = 0,
                AverageOrderValue = 0,
                FirstUsed = null,
                LastUsed = null,
                IsActive = discount.IsActive,
                StoreId = discount.ApplicableStoreId,
                StoreName = discount.Store?.Name
            };
        }

        private DiscountPerformance CreateEmptyPerformance(Domain.Entities.Catalog.Discount discount, DateTime startDate, DateTime endDate)
        {
            return new DiscountPerformance
            {
                DiscountCode = discount.Code,
                DiscountType = discount.Type,
                DiscountValue = discount.Value,
                OrderCount = 0,
                TotalRevenue = 0,
                TotalDiscountGiven = 0,
                NetRevenue = 0,
                ConversionRate = 0,
                AverageOrderValue = 0,
                AverageDiscountPerOrder = 0,
                StartDate = startDate,
                EndDate = endDate,
                StoreId = discount.ApplicableStoreId,
                StoreName = discount.Store?.Name
            };
        }
    }
}
