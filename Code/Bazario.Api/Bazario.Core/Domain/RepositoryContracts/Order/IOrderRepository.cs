using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Store;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;

namespace Bazario.Core.Domain.RepositoryContracts.Order
{
    public interface IOrderRepository
    {
        Task<OrderEntity> AddOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);

        Task<OrderEntity> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes an order by setting IsDeleted = true
        /// </summary>
        Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted order by setting IsDeleted = false
        /// </summary>
        Task<bool> RestoreOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an order by ID including soft-deleted orders (ignores query filter)
        /// </summary>
        Task<OrderEntity?> GetOrderByIdIncludeDeletedAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hard deletes an order (permanently removes from database)
        /// </summary>
        Task<bool> DeleteOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<OrderEntity?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetOrdersByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetFilteredOrdersAsync(Expression<Func<OrderEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);

        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated order statistics for a specific store within a date range
        /// </summary>
        Task<StoreOrderStats> GetStoreOrderStatsAsync(Guid storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets store order statistics for multiple stores in a single query
        /// </summary>
        Task<Dictionary<Guid, StoreOrderStats>> GetBulkStoreOrderStatsAsync(List<Guid> storeIds, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all orders that used a specific discount code
        /// </summary>
        Task<List<OrderEntity>> GetOrdersByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets orders with pre-calculated discount code counts for performance optimization
        /// </summary>
        Task<List<OrderWithCodeCount>> GetOrdersWithCodeCountsByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets orders by discount code within a date range
        /// </summary>
        Task<List<OrderEntity>> GetOrdersByDiscountCodeAndDateRangeAsync(string discountCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all orders with discounts within a date range
        /// </summary>
        Task<List<OrderEntity>> GetOrdersWithDiscountsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets orders count by discount code
        /// </summary>
        Task<int> GetOrderCountByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of orders within a date range
        /// </summary>
        Task<int> GetOrdersCountByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product exists in any orders with the specified statuses (optimized for performance)
        /// Uses EXISTS query instead of loading all orders into memory
        /// </summary>
        /// <param name="productId">Product ID to check</param>
        /// <param name="statuses">Order statuses to check (e.g., Pending, Processing, Shipped)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product exists in any order with specified statuses, false otherwise</returns>
        Task<bool> HasProductInOrdersWithStatusAsync(Guid productId, string[] statuses, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product exists in any orders with the specified statuses and date range (optimized for performance)
        /// Uses EXISTS query instead of loading all orders into memory
        /// </summary>
        /// <param name="productId">Product ID to check</param>
        /// <param name="statuses">Order statuses to check</param>
        /// <param name="startDate">Start date for filtering orders</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if product exists in orders matching criteria, false otherwise</returns>
        Task<bool> HasProductInOrdersWithStatusAndDateAsync(Guid productId, string[] statuses, DateTime startDate, CancellationToken cancellationToken = default);

        // ========== BULK ANALYTICS METHODS (Performance Optimized) ==========

        /// <summary>
        /// Gets aggregated order statistics for multiple discount codes in a single query.
        /// Uses database-level GROUP BY aggregation to avoid N+1 query problems.
        /// Returns only aggregated data - no full Order entities loaded.
        /// </summary>
        /// <param name="discountCodes">List of discount codes to get statistics for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of aggregated statistics per discount code</returns>
        Task<List<OrderDiscountStats>> GetOrderStatsByDiscountCodesAsync(
            List<string> discountCodes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated order statistics for multiple discount codes within a date range.
        /// Uses database-level GROUP BY aggregation with date filtering.
        /// Returns only aggregated data - no full Order entities loaded.
        /// </summary>
        /// <param name="discountCodes">List of discount codes to get statistics for</param>
        /// <param name="startDate">Start date for filtering orders</param>
        /// <param name="endDate">End date for filtering orders</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of aggregated statistics per discount code</returns>
        Task<List<OrderDiscountStats>> GetOrderStatsByDiscountCodesAndDateRangeAsync(
            List<string> discountCodes,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated revenue impact statistics for all orders within a date range.
        /// Uses database-level aggregation with CASE statements to efficiently calculate
        /// discounted vs non-discounted order metrics without loading full Order entities.
        /// </summary>
        /// <param name="startDate">Start date for filtering orders</param>
        /// <param name="endDate">End date for filtering orders</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Aggregated revenue impact statistics</returns>
        Task<RevenueImpactStats> GetRevenueImpactStatsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}
