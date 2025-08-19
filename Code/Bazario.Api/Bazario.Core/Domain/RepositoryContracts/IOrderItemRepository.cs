using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface IOrderItemRepository
    {
        Task<OrderItem> AddOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default);

        Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default);

        Task<bool> DeleteOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);

        Task<OrderItem?> GetOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);

        Task<List<OrderItem>> GetAllOrderItemsAsync(CancellationToken cancellationToken = default);

        Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<List<OrderItem>> GetOrderItemsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<List<OrderItem>> GetFilteredOrderItemsAsync(Expression<Func<OrderItem, bool>> predicate, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalValueByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<int> GetTotalQuantityByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<int> GetOrderItemCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<bool> DeleteOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}
