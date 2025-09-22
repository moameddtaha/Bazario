using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Enums;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface IOrderRepository
    {
        Task<Order> AddOrderAsync(Order order, CancellationToken cancellationToken = default);

        Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);

        Task<bool> DeleteOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

        Task<List<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

        Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<List<Order>> GetFilteredOrdersAsync(Expression<Func<Order, bool>> predicate, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);

        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<int> GetOrderCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated order statistics for a specific store within a date range
        /// </summary>
        Task<StoreOrderStats> GetStoreOrderStatsAsync(Guid storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
