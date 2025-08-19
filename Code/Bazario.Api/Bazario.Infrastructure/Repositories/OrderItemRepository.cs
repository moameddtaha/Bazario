using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderItemRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<OrderItem> AddOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderItem == null)
                    throw new ArgumentNullException(nameof(orderItem));

                // Add order item to context
                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync(cancellationToken);

                return orderItem;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating order item: {ex.Message}", ex);
            }
        }

        public async Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderItem == null)
                    throw new ArgumentNullException(nameof(orderItem));

                if (orderItem.OrderItemId == Guid.Empty)
                    throw new ArgumentException("Order Item ID cannot be empty", nameof(orderItem));

                // Check if order item exists (use FindAsync for simple PK lookup)
                var existingOrderItem = await _context.OrderItems.FindAsync(new object[] { orderItem.OrderItemId }, cancellationToken);
                if (existingOrderItem == null)
                {
                    throw new InvalidOperationException($"Order Item with ID {orderItem.OrderItemId} not found");
                }

                // Update only specific properties (not foreign keys or primary key)
                existingOrderItem.Quantity = orderItem.Quantity;
                existingOrderItem.Price = orderItem.Price;
                
                await _context.SaveChangesAsync(cancellationToken);

                return existingOrderItem;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while updating order item with ID {orderItem?.OrderItemId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderItemId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var orderItem = await _context.OrderItems.FindAsync(new object[] { orderItemId }, cancellationToken);
                if (orderItem == null)
                {
                    return false; // Order item not found
                }

                // Delete the order item
                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting order item with ID {orderItemId}: {ex.Message}", ex);
            }
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderItemId == Guid.Empty)
                {
                    return null; // Invalid ID
                }

                // Find the order item with navigation properties
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken);

                return orderItem;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve order item with ID {orderItemId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetAllOrderItemsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .ToListAsync(cancellationToken);

                return orderItems;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve order items: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return new List<OrderItem>(); // Invalid ID, return empty list
                }

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(cancellationToken);

                return orderItems;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve order items for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return new List<OrderItem>(); // Invalid ID, return empty list
                }

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.ProductId == productId)
                    .ToListAsync(cancellationToken);

                return orderItems;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve order items for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetFilteredOrderItemsAsync(Expression<Func<OrderItem, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                return orderItems;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered order items: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalValueByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var totalValue = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .SumAsync(oi => oi.Quantity * oi.Price, cancellationToken);

                return totalValue;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate total value for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalQuantityByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var totalQuantity = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .SumAsync(oi => oi.Quantity, cancellationToken);

                return totalQuantity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate total quantity for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderItemCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var count = await _context.OrderItems
                    .CountAsync(oi => oi.ProductId == productId, cancellationToken);

                return count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count order items for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(cancellationToken);

                if (!orderItems.Any())
                {
                    return true; // No items to delete, consider it successful
                }

                _context.OrderItems.RemoveRange(orderItems);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete order items for order {orderId}: {ex.Message}", ex);
            }
        }
    }
}
