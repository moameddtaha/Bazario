using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Order;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.Helpers.Order;
using Microsoft.Extensions.Logging;
using Bazario.Core.Enums;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Models.Catalog.Discount;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order analytics and reporting
    /// Handles order analytics, revenue calculations, and performance metrics
    /// </summary>
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderMetricsHelper _metricsHelper;
        private readonly IDiscountRepository _discountRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly ILogger<OrderAnalyticsService> _logger;

        public OrderAnalyticsService(
            IOrderRepository orderRepository,
            IOrderMetricsHelper metricsHelper,
            IDiscountRepository discountRepository,
            IStoreRepository storeRepository,
            ILogger<OrderAnalyticsService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _metricsHelper = metricsHelper ?? throw new ArgumentNullException(nameof(metricsHelper));
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerOrderAnalytics> GetCustomerOrderAnalyticsAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting order analytics for customer: {CustomerId}", customerId);

            try
            {
                // Materialize immediately to avoid multiple enumerations
                var ordersList = (await _orderRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken)).ToList();

                var analytics = new CustomerOrderAnalytics
                {
                    CustomerId = customerId,
                    TotalOrders = ordersList.Count,
                    TotalSpent = ordersList.Sum(o => o.TotalAmount),
                    AverageOrderValue = ordersList.Count > 0 ? ordersList.Average(o => o.TotalAmount) : 0,
                    FirstOrderDate = ordersList.Count > 0 ? ordersList.Min(o => o.Date) : (DateTime?)null,
                    LastOrderDate = ordersList.Count > 0 ? ordersList.Max(o => o.Date) : (DateTime?)null,
                    PendingOrders = ordersList.Count(o => o.Status == "Pending"),
                    CompletedOrders = ordersList.Count(o => o.Status == "Delivered"),
                    CancelledOrders = ordersList.Count(o => o.Status == "Cancelled"),
                    MonthlyData = ordersList
                        .GroupBy(o => new { o.Date.Year, o.Date.Month })
                        .Select(g => new MonthlyOrderData
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Orders = g.Count(),
                            TotalAmount = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(m => m.Year)
                        .ThenBy(m => m.Month)
                        .ToList()
                };

                _logger.LogDebug("Successfully retrieved customer order analytics for: {CustomerId}, Total Orders: {TotalOrders}",
                    customerId, analytics.TotalOrders);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer order analytics: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting revenue analytics from {StartDate} to {EndDate}", startDate, endDate);

            try
            {
                // Materialize immediately to avoid multiple enumerations
                var ordersList = (await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken)).ToList();

                var analytics = new RevenueAnalytics
                {
                    TotalRevenue = ordersList.Sum(o => o.TotalAmount),
                    TotalOrders = ordersList.Count,
                    AverageOrderValue = ordersList.Count > 0 ? ordersList.Average(o => o.TotalAmount) : 0,
                    HighestOrderValue = ordersList.Count > 0 ? ordersList.Max(o => o.TotalAmount) : 0,
                    LowestOrderValue = ordersList.Count > 0 ? ordersList.Min(o => o.TotalAmount) : 0,
                    MonthlyData = ordersList
                        .GroupBy(o => new { o.Date.Year, o.Date.Month })
                        .Select(g => new MonthlyRevenueData
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Revenue = g.Sum(o => o.TotalAmount),
                            Orders = g.Count()
                        })
                        .OrderBy(m => m.Year)
                        .ThenBy(m => m.Month)
                        .ToList()
                };

                _logger.LogDebug("Successfully retrieved revenue analytics. Total Revenue: {TotalRevenue}, Total Orders: {TotalOrders}",
                    analytics.TotalRevenue, analytics.TotalOrders);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get revenue analytics");
                throw;
            }
        }

        public async Task<OrderPerformanceMetrics> GetOrderPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting order performance metrics from {StartDate} to {EndDate}", startDate, endDate);

            try
            {
                // Materialize immediately to avoid multiple enumerations
                var ordersList = (await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken)).ToList();

                var totalOrders = ordersList.Count;
                
                // Calculate status counts once to avoid redundant counting
                var deliveredCount = ordersList.Count(o => o.Status == "Delivered");
                var cancelledCount = ordersList.Count(o => o.Status == "Cancelled");

                var metrics = new OrderPerformanceMetrics
                {
                    TotalOrders = totalOrders,
                    PendingOrders = ordersList.Count(o => o.Status == "Pending"),
                    ProcessingOrders = ordersList.Count(o => o.Status == "Processing"),
                    ShippedOrders = ordersList.Count(o => o.Status == "Shipped"),
                    DeliveredOrders = deliveredCount,
                    CancelledOrders = cancelledCount,
                    AverageProcessingTime = _metricsHelper.CalculateAverageProcessingTime(ordersList),
                    AverageDeliveryTime = _metricsHelper.CalculateAverageDeliveryTime(ordersList),
                    OrderFulfillmentRate = totalOrders > 0 
                        ? (decimal)deliveredCount / totalOrders * 100 
                        : 0,
                    CancellationRate = totalOrders > 0 
                        ? (decimal)cancelledCount / totalOrders * 100 
                        : 0
                };

                _logger.LogDebug("Successfully retrieved order performance metrics. Total Orders: {TotalOrders}, Fulfillment Rate: {FulfillmentRate}%",
                    metrics.TotalOrders, metrics.OrderFulfillmentRate);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order performance metrics");
                throw;
            }
        }

        public async Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount usage stats for code: {DiscountCode}", discountCode);

            try
            {
                // Get orders with pre-calculated code counts for optimal performance
                var ordersWithCodeCounts = await _orderRepository.GetOrdersWithCodeCountsByDiscountCodeAsync(discountCode, cancellationToken);

                if (ordersWithCodeCounts.Count == 0)
                {
                    _logger.LogDebug("No orders found for discount code: {DiscountCode}", discountCode);
                    return null;
                }

                // Get discount details from repository
                var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode, cancellationToken);
                
                // Get store information if applicable
                string? storeName = null;
                if (discount?.ApplicableStoreId != null)
                {
                    var store = await _storeRepository.GetStoreByIdAsync(discount.ApplicableStoreId.Value, cancellationToken);
                    storeName = store?.Name;
                }

                // Use pre-calculated proportional amounts from repository
                var firstUsed = ordersWithCodeCounts.Min(o => o.Order.Date);
                var lastUsed = ordersWithCodeCounts.Max(o => o.Order.Date);

                var stats = new DiscountUsageStats
                {
                    DiscountCode = discountCode,
                    UsageCount = ordersWithCodeCounts.Count,
                    TotalDiscountAmount = ordersWithCodeCounts.Sum(o => o.ProportionalDiscountAmount),
                    AverageDiscountAmount = ordersWithCodeCounts.Average(o => o.ProportionalDiscountAmount),
                    TotalRevenue = ordersWithCodeCounts.Sum(o => o.ProportionalTotalAmount),
                    AverageOrderValue = ordersWithCodeCounts.Average(o => o.ProportionalTotalAmount),
                    FirstUsed = firstUsed,
                    LastUsed = lastUsed,
                    IsActive = discount?.IsActive ?? false,
                    StoreId = discount?.ApplicableStoreId,
                    StoreName = storeName
                };

                _logger.LogDebug("Successfully retrieved discount usage stats for {DiscountCode}. Usage Count: {UsageCount}, Total Discount: {TotalDiscount}",
                    discountCode, stats.UsageCount, stats.TotalDiscountAmount);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get discount usage stats for code: {DiscountCode}", discountCode);
                throw;
            }
        }

        public async Task<List<DiscountPerformance>> GetDiscountPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount performance from {StartDate} to {EndDate}", startDate, endDate);

            try
            {
                var orders = await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken);
                var ordersWithDiscounts = orders.Where(o => !string.IsNullOrEmpty(o.AppliedDiscountCodes)).ToList();

                // Extract and group discount codes with their orders, pre-calculating code counts
                var discountGroups = ordersWithDiscounts
                    .SelectMany(o => {
                        var codes = o.AppliedDiscountCodes!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(c => c.Trim())
                            .Where(c => !string.IsNullOrEmpty(c))
                            .ToList();
                        
                        var codeCount = codes.Count;
                        return codes.Select(code => new { 
                            Order = o, 
                            DiscountCode = code,
                            CodeCount = codeCount
                        });
                    })
                    .GroupBy(x => x.DiscountCode)
                    .ToList();

                // Get unique discount codes (Distinct is redundant after GroupBy)
                var uniqueDiscountCodes = discountGroups.Select(g => g.Key).ToList();
                
                // Batch fetch all discounts to avoid N+1 problem
                var discounts = await _discountRepository.GetDiscountsByCodesAsync(uniqueDiscountCodes, cancellationToken);
                var discountDict = discounts.ToDictionary(d => d.Code, d => d);
                
                // Get unique store IDs and batch fetch stores
                var storeIds = discounts.Where(d => d.ApplicableStoreId.HasValue)
                    .Select(d => d.ApplicableStoreId!.Value)
                    .Distinct()
                    .ToList();
                
                var stores = storeIds.Any() 
                    ? await _storeRepository.GetStoresByIdsAsync(storeIds, cancellationToken)
                    : new List<Domain.Entities.Store.Store>();
                var storeDict = stores.ToDictionary(s => s.StoreId, s => s);

                // Calculate total orders for proper conversion rate
                var totalOrders = orders.Count;

                // Log missing discount codes for debugging
                var missingCodes = discountGroups
                    .Where(g => !discountDict.ContainsKey(g.Key))
                    .Select(g => g.Key)
                    .ToList();
                
                if (missingCodes.Any())
                {
                    _logger.LogWarning("Found {Count} discount codes in orders that don't exist in database: {MissingCodes}", 
                        missingCodes.Count, string.Join(", ", missingCodes));
                }

                // Log missing stores for debugging
                var missingStoreIds = discountDict.Values
                    .Where(d => d.ApplicableStoreId.HasValue && !storeDict.ContainsKey(d.ApplicableStoreId.Value))
                    .Select(d => d.ApplicableStoreId!.Value)
                    .ToList();
                
                if (missingStoreIds.Any())
                {
                    _logger.LogWarning("Found {Count} store IDs referenced by discounts that don't exist in database: {MissingStoreIds}", 
                        missingStoreIds.Count, string.Join(", ", missingStoreIds));
                }

                // Convert groups to DiscountPerformance objects
                var discountPerformance = discountGroups
                    .Where(g => discountDict.ContainsKey(g.Key)) // Filter out missing discount codes
                    .Select(g => 
                    {
                        var discount = discountDict[g.Key]; // Safe to use indexer since we filtered above
                        var store = discount.ApplicableStoreId != null 
                            ? storeDict.GetValueOrDefault(discount.ApplicableStoreId.Value)
                            : null;

                        // Use pre-calculated code counts to avoid repeated string splitting
                        var orderCount = g.Count();
                        
                        var totalOrderAmount = g.Sum(x => 
                            x.CodeCount > 0 ? x.Order.TotalAmount / x.CodeCount : 0);
                        
                        var totalDiscountAmount = g.Sum(x => 
                            x.CodeCount > 0 ? x.Order.DiscountAmount / x.CodeCount : 0);

                        return new DiscountPerformance
                        {
                            DiscountCode = g.Key,
                            DiscountType = discount.Type,
                            DiscountValue = discount.Value,
                            OrderCount = orderCount,
                            TotalRevenue = totalOrderAmount, // Correctly proportional
                            TotalDiscountGiven = totalDiscountAmount, // Correctly proportional
                            NetRevenue = totalOrderAmount - totalDiscountAmount,
                            ConversionRate = totalOrders > 0 ? (decimal)orderCount / totalOrders * 100 : 0, // True conversion rate
                            AverageOrderValue = g.Average(x => 
                                x.CodeCount > 0 ? x.Order.TotalAmount / x.CodeCount : 0),
                            AverageDiscountPerOrder = g.Average(x => 
                                x.CodeCount > 0 ? x.Order.DiscountAmount / x.CodeCount : 0),
                            StartDate = startDate,
                            EndDate = endDate,
                            StoreId = discount.ApplicableStoreId, // No ? needed since we filtered above
                            StoreName = store?.Name
                        };
                    })
                    .OrderByDescending(d => d.TotalRevenue)
                    .ToList();

                _logger.LogDebug("Successfully retrieved discount performance. Found {DiscountCount} discount codes",
                    discountPerformance.Count);

                return discountPerformance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get discount performance");
                throw;
            }
        }

        public async Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount revenue impact from {StartDate} to {EndDate}", startDate, endDate);

            try
            {
                var ordersList = (await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken)).ToList();
                var ordersWithDiscounts = ordersList.Where(o => !string.IsNullOrEmpty(o.AppliedDiscountCodes)).ToList();
                var ordersWithoutDiscounts = ordersList.Where(o => string.IsNullOrEmpty(o.AppliedDiscountCodes)).ToList();

                var totalRevenue = ordersList.Sum(o => o.TotalAmount);
                var discountedOrderRevenue = ordersWithDiscounts.Sum(o => o.TotalAmount);
                var nonDiscountedOrderRevenue = ordersWithoutDiscounts.Sum(o => o.TotalAmount);
                var totalDiscountsGiven = ordersWithDiscounts.Sum(o => o.DiscountAmount);

                // Calculate proportional breakdowns to avoid double-counting
                var discountBreakdowns = ordersWithDiscounts
                    .SelectMany(o => {
                        var codes = o.AppliedDiscountCodes!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(c => c.Trim())
                            .Where(c => !string.IsNullOrEmpty(c))
                            .ToList();
                        
                        var codeCount = codes.Count;
                        return codes.Select(code => new { 
                            Order = o, 
                            DiscountCode = code,
                            CodeCount = codeCount
                        });
                    })
                    .GroupBy(x => x.DiscountCode)
                    .Select(g => new DiscountRevenueBreakdown
                    {
                        DiscountCode = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(x => x.Order.TotalAmount / x.CodeCount),
                        DiscountAmount = g.Sum(x => x.Order.DiscountAmount / x.CodeCount),
                        NetRevenue = g.Sum(x => (x.Order.TotalAmount - x.Order.DiscountAmount) / x.CodeCount),
                        AverageOrderValue = g.Average(x => x.Order.TotalAmount / x.CodeCount)
                    })
                    .OrderByDescending(b => b.Revenue)
                    .ToList();

                var impact = new DiscountRevenueImpact
                {
                    TotalRevenue = totalRevenue,
                    DiscountedOrderRevenue = discountedOrderRevenue,
                    NonDiscountedOrderRevenue = nonDiscountedOrderRevenue,
                    TotalDiscountsGiven = totalDiscountsGiven,
                    NetRevenue = totalRevenue - totalDiscountsGiven,
                    DiscountedRevenuePercentage = totalRevenue > 0 ? (discountedOrderRevenue / totalRevenue) * 100 : 0,
                    AverageDiscountedOrderValue = ordersWithDiscounts.Any() ? ordersWithDiscounts.Average(o => o.TotalAmount) : 0,
                    AverageNonDiscountedOrderValue = ordersWithoutDiscounts.Any() ? ordersWithoutDiscounts.Average(o => o.TotalAmount) : 0,
                    DiscountedOrderCount = ordersWithDiscounts.Count,
                    NonDiscountedOrderCount = ordersWithoutDiscounts.Count,
                    TotalOrderCount = ordersList.Count,
                    DiscountUsageRate = ordersList.Count > 0 ? (decimal)ordersWithDiscounts.Count / ordersList.Count * 100 : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    DiscountBreakdowns = discountBreakdowns
                };

                _logger.LogDebug("Successfully retrieved discount revenue impact. Total Revenue: {TotalRevenue}, Discount Usage Rate: {UsageRate}%",
                    impact.TotalRevenue, impact.DiscountUsageRate);

                return impact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get discount revenue impact");
                throw;
            }
        }
    }
}
