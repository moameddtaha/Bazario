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
    /// </summary>
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

        public async Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting usage stats for discount code: {Code}", discountCode);

            var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode, cancellationToken);
            if (discount == null)
            {
                _logger.LogWarning("Discount not found: {Code}", discountCode);
                return null;
            }

            var orderCount = await _orderRepository.GetOrderCountByDiscountCodeAsync(discountCode, cancellationToken);

            if (orderCount == 0)
            {
                return CreateEmptyUsageStats(discountCode, discount);
            }

            // Get all orders with this discount code to calculate stats
            var orders = await _orderRepository.GetOrdersByDiscountCodeAsync(discountCode, cancellationToken);

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

        public async Task<List<DiscountUsageStats>> GetAllDiscountUsageStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting usage stats for all discounts");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var discounts = await _discountRepository.GetValidDiscountsAsync(startDate.Value, endDate.Value, cancellationToken);
            var statsList = new List<DiscountUsageStats>();

            foreach (var discount in discounts)
            {
                var stats = await GetDiscountUsageStatsAsync(discount.Code, cancellationToken);
                if (stats != null)
                {
                    statsList.Add(stats);
                }
            }

            return statsList.OrderByDescending(s => s.TotalRevenue).ToList();
        }

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

            var totalOrderCount = await _orderRepository.GetOrdersCountByDateRangeAsync(
                startDate.Value, endDate.Value, cancellationToken);

            if (!discountOrders.Any())
            {
                return CreateEmptyPerformance(discount, startDate.Value, endDate.Value);
            }

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

        public async Task<List<DiscountPerformance>> GetAllDiscountPerformanceAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting performance metrics for all discounts");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var discounts = await _discountRepository.GetValidDiscountsAsync(startDate.Value, endDate.Value, cancellationToken);
            var performanceList = new List<DiscountPerformance>();

            foreach (var discount in discounts)
            {
                var performance = await GetDiscountPerformanceAsync(discount.Code, startDate, endDate, cancellationToken);
                if (performance != null && performance.OrderCount > 0)
                {
                    performanceList.Add(performance);
                }
            }

            return performanceList.OrderByDescending(p => p.TotalRevenue).ToList();
        }

        public async Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount revenue impact analysis");

            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var allOrders = await _orderRepository.GetOrdersByDateRangeAsync(startDate.Value, endDate.Value, cancellationToken);
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
            var statsList = new List<DiscountUsageStats>();

            foreach (var discount in storeDiscounts)
            {
                var stats = await GetDiscountUsageStatsAsync(discount.Code, cancellationToken);
                if (stats != null)
                {
                    statsList.Add(stats);
                }
            }

            return statsList.OrderByDescending(s => s.TotalRevenue).ToList();
        }

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
