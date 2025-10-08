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

        Task<bool> DeleteOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<OrderEntity?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

        Task<List<OrderEntity>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

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
    }
}
