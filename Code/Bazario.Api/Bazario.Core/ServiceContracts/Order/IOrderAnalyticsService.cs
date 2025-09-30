using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order analytics and reporting
    /// Handles order analytics, revenue calculations, and performance metrics
    /// </summary>
    public interface IOrderAnalyticsService
    {
        /// <summary>
        /// Gets comprehensive order analytics for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer order analytics data</returns>
        Task<CustomerOrderAnalytics> GetCustomerOrderAnalyticsAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets revenue statistics within a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Revenue analytics data</returns>
        Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets order performance metrics
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order performance metrics</returns>
        Task<OrderPerformanceMetrics> GetOrderPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
