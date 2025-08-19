using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Order> AddOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (order == null)
                    throw new ArgumentNullException(nameof(order));

                // Add order to context
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                return order;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating order: {ex.Message}", ex);
            }
        }

        public async Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (order == null)
                    throw new ArgumentNullException(nameof(order));

                if (order.OrderId == Guid.Empty)
                    throw new ArgumentException("Order ID cannot be empty", nameof(order));

                // Check if order exists (use FindAsync for simple PK lookup)
                var existingOrder = await _context.Orders.FindAsync(new object[] { order.OrderId }, cancellationToken);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {order.OrderId} not found");
                }

                // Update only specific properties (not foreign keys or primary key)
                existingOrder.Date = order.Date;
                existingOrder.TotalAmount = order.TotalAmount;
                existingOrder.Status = order.Status;
                
                await _context.SaveChangesAsync(cancellationToken);

                return existingOrder;
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
                throw new InvalidOperationException($"Unexpected error while updating order with ID {order?.OrderId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return false; // Order not found
                }

                // Delete the order
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    return null; // Invalid ID
                }

                // Find the order with navigation properties
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                return order;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ToListAsync(cancellationToken);

                return orders;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve orders: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    return new List<Order>(); // Invalid ID, return empty list
                }

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => o.CustomerId == customerId)
                    .ToListAsync(cancellationToken);

                return orders;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve orders for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var statusString = status.ToString();
                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => o.Status == statusString)
                    .ToListAsync(cancellationToken);

                return orders;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve orders with status {status}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate date range
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be greater than end date");
                }

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .ToListAsync(cancellationToken);

                return orders;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve orders for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetFilteredOrdersAsync(Expression<Func<Order, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                return orders;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered orders: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var totalRevenue = await _context.Orders
                    .SumAsync(o => o.TotalAmount, cancellationToken);

                return totalRevenue;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate total revenue: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate date range
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be greater than end date");
                }

                var totalRevenue = await _context.Orders
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .SumAsync(o => o.TotalAmount, cancellationToken);

                return totalRevenue;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate total revenue for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var statusString = status.ToString();
                var count = await _context.Orders
                    .CountAsync(o => o.Status == statusString, cancellationToken);

                return count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count orders with status {status}: {ex.Message}", ex);
            }
        }
    }
}
