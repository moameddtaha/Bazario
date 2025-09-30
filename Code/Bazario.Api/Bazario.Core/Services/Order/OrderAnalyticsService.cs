using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Order;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order analytics and reporting
    /// Handles order analytics, revenue calculations, and performance metrics
    /// </summary>
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderAnalyticsService> _logger;

        public OrderAnalyticsService(
            IOrderRepository orderRepository,
            ILogger<OrderAnalyticsService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerOrderAnalytics> GetCustomerOrderAnalyticsAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting order analytics for customer: {CustomerId}", customerId);

            try
            {
                var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken);

                var analytics = new CustomerOrderAnalytics
                {
                    CustomerId = customerId,
                    TotalOrders = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalAmount),
                    AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    FirstOrderDate = orders.Any() ? orders.Min(o => o.Date) : null,
                    LastOrderDate = orders.Any() ? orders.Max(o => o.Date) : null,
                    PendingOrders = orders.Count(o => o.Status == "Pending"),
                    CompletedOrders = orders.Count(o => o.Status == "Delivered"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                    MonthlyData = orders
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
                var orders = await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken);

                var analytics = new RevenueAnalytics
                {
                    TotalRevenue = orders.Sum(o => o.TotalAmount),
                    TotalOrders = orders.Count,
                    AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    HighestOrderValue = orders.Any() ? orders.Max(o => o.TotalAmount) : 0,
                    LowestOrderValue = orders.Any() ? orders.Min(o => o.TotalAmount) : 0,
                    MonthlyData = orders
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
                var orders = await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate, cancellationToken);

                var totalOrders = orders.Count;
                var metrics = new OrderPerformanceMetrics
                {
                    TotalOrders = totalOrders,
                    PendingOrders = orders.Count(o => o.Status == "Pending"),
                    ProcessingOrders = orders.Count(o => o.Status == "Processing"),
                    ShippedOrders = orders.Count(o => o.Status == "Shipped"),
                    DeliveredOrders = orders.Count(o => o.Status == "Delivered"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                    AverageProcessingTime = 24, // Placeholder - would need more data to calculate
                    AverageDeliveryTime = 72, // Placeholder - would need more data to calculate
                    OrderFulfillmentRate = totalOrders > 0 
                        ? (decimal)orders.Count(o => o.Status == "Delivered") / totalOrders * 100 
                        : 0,
                    CancellationRate = totalOrders > 0 
                        ? (decimal)orders.Count(o => o.Status == "Cancelled") / totalOrders * 100 
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
    }
}
