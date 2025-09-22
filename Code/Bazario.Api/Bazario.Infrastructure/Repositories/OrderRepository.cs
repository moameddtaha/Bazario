using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums;
using Bazario.Core.Models.Store;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(ApplicationDbContext context, ILogger<OrderRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Order> AddOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new order for customer: {CustomerId}", order?.CustomerId);
            
            try
            {
                // Validate input
                if (order == null)
                {
                    _logger.LogWarning("Attempted to add null order");
                    throw new ArgumentNullException(nameof(order));
                }

                _logger.LogDebug("Adding order to database context. OrderId: {OrderId}, Total: {OrderTotal}, Status: {OrderStatus}", 
                    order.OrderId, order.TotalAmount, order.Status);

                // Add order to context
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added order. OrderId: {OrderId}, Total: {OrderTotal}", 
                    order.OrderId, order.TotalAmount);

                return order;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding order for customer: {CustomerId}", order?.CustomerId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating order for customer: {CustomerId}", order?.CustomerId);
                throw new InvalidOperationException($"Unexpected error while creating order: {ex.Message}", ex);
            }
        }

        public async Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update order: {OrderId}", order?.OrderId);
            
            try
            {
                // Validate input
                if (order == null)
                {
                    _logger.LogWarning("Attempted to update null order");
                    throw new ArgumentNullException(nameof(order));
                }

                if (order.OrderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update order with empty ID");
                    throw new ArgumentException("Order ID cannot be empty", nameof(order));
                }

                _logger.LogDebug("Checking if order exists in database. OrderId: {OrderId}", order.OrderId);

                // Check if order exists (use FindAsync for simple PK lookup)
                var existingOrder = await _context.Orders.FindAsync(new object[] { order.OrderId }, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order not found for update. OrderId: {OrderId}", order.OrderId);
                    throw new InvalidOperationException($"Order with ID {order.OrderId} not found");
                }

                _logger.LogDebug("Updating order properties. OrderId: {OrderId}, Date: {Date}, TotalAmount: {TotalAmount}, Status: {Status}", 
                    order.OrderId, order.Date, order.TotalAmount, order.Status);

                existingOrder.Date = order.Date;

                if (order.TotalAmount > 0) // Only update if total amount is valid
                {
                    existingOrder.TotalAmount = order.TotalAmount;
                }

                if (!string.IsNullOrEmpty(order.Status)) // Only update if status is provided
                {
                    existingOrder.Status = order.Status;
                }
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated order. OrderId: {OrderId}, TotalAmount: {TotalAmount}, Status: {Status}", 
                    order.OrderId, order.TotalAmount, order.Status);

                return existingOrder;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating order: {OrderId}", order?.OrderId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating order: {OrderId}", order?.OrderId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating order: {OrderId}", order?.OrderId);
                throw new InvalidOperationException($"Unexpected error while updating order with ID {order?.OrderId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete order with empty ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Checking if order exists for deletion. OrderId: {OrderId}", orderId);

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for deletion. OrderId: {OrderId}", orderId);
                    return false; // Order not found
                }

                _logger.LogDebug("Removing order from database context. OrderId: {OrderId}", orderId);

                // Delete the order
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted order. OrderId: {OrderId}", orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting order: {OrderId}", orderId);
                throw new InvalidOperationException($"Unexpected error while deleting order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve order by ID: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve order with empty ID");
                    return null; // Invalid ID
                }

                _logger.LogDebug("Querying order with navigation properties. OrderId: {OrderId}", orderId);

                // Find the order with navigation properties
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogDebug("Order not found. OrderId: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved order. OrderId: {OrderId}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}", 
                        order.OrderId, order.CustomerId, order.TotalAmount);
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order: {OrderId}", orderId);
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
                _logger.LogError(ex, "Failed to retrieve all orders");
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
                    _logger.LogWarning("Attempted to retrieve orders with empty customer ID");
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
                _logger.LogError(ex, "Failed to retrieve orders for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to retrieve orders for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve orders by status: {Status}", status);
            
            try
            {
                var statusString = status.ToString();
                _logger.LogDebug("Querying orders with status: {StatusString}", statusString);

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => o.Status == statusString)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderCount} orders with status: {Status}", orders.Count, status);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders with status: {Status}", status);
                throw new InvalidOperationException($"Failed to retrieve orders with status {status}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve orders by date range: {StartDate} to {EndDate}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            
            try
            {
                // Validate date range
                if (startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range: start date {StartDate} is greater than end date {EndDate}", 
                        startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    throw new ArgumentException("Start date cannot be greater than end date");
                }

                _logger.LogDebug("Querying orders within date range with navigation properties");

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderCount} orders for date range: {StartDate} to {EndDate}", 
                    orders.Count, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                return orders;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving orders by date range: {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for date range: {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw new InvalidOperationException($"Failed to retrieve orders for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetFilteredOrdersAsync(Expression<Func<Order, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered orders");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve orders with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Querying filtered orders with navigation properties");

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderCount} filtered orders", orders.Count);

                return orders;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered orders");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered orders");
                throw new InvalidOperationException($"Failed to retrieve filtered orders: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate total revenue");
            
            try
            {
                _logger.LogDebug("Calculating sum of all order total amounts");

                var totalRevenue = await _context.Orders
                    .SumAsync(o => o.TotalAmount, cancellationToken);

                _logger.LogDebug("Successfully calculated total revenue: {TotalRevenue:C}", totalRevenue);

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate total revenue");
                throw new InvalidOperationException($"Failed to calculate total revenue: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate total revenue for date range: {StartDate} to {EndDate}", 
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            
            try
            {
                // Validate date range
                if (startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range: start date {StartDate} is greater than end date {EndDate}", 
                        startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    throw new ArgumentException("Start date cannot be greater than end date");
                }

                _logger.LogDebug("Calculating sum of order total amounts within date range");

                var totalRevenue = await _context.Orders
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .SumAsync(o => o.TotalAmount, cancellationToken);

                _logger.LogDebug("Successfully calculated total revenue for date range: {TotalRevenue:C} ({StartDate} to {EndDate})", 
                    totalRevenue, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                return totalRevenue;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while calculating total revenue for date range: {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate total revenue for date range: {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw new InvalidOperationException($"Failed to calculate total revenue for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count orders by status: {Status}", status);
            
            try
            {
                var statusString = status.ToString();
                _logger.LogDebug("Counting orders with status: {StatusString}", statusString);

                var count = await _context.Orders
                    .CountAsync(o => o.Status == statusString, cancellationToken);

                _logger.LogDebug("Successfully counted orders with status {Status}: {Count}", status, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count orders with status: {Status}", status);
                throw new InvalidOperationException($"Failed to count orders with status {status}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count orders for store: {StoreId}", storeId);
            
            try
            {
                // Count orders that have order items with products from this store
                // Order -> OrderItem -> Product -> Store
                var count = await _context.Orders
                    .Where(o => o.OrderItems != null && 
                               o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId))
                    .CountAsync(cancellationToken);

                _logger.LogDebug("Successfully counted orders for store {StoreId}: {Count}", storeId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count orders for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to count orders for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Counting orders for product: {ProductId}", productId);

            try
            {
                // Count orders that have order items with this specific product
                // Order -> OrderItem -> Product
                var count = await _context.Orders
                    .Where(o => o.OrderItems != null && 
                               o.OrderItems.Any(oi => oi.ProductId == productId))
                    .CountAsync(cancellationToken);

                _logger.LogDebug("Successfully counted orders for product {ProductId}: {Count}", productId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count orders for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to count orders for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<StoreOrderStats> GetStoreOrderStatsAsync(Guid storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting store order stats for store: {StoreId}, from {StartDate} to {EndDate}", storeId, startDate, endDate);
            
            try
            {
                // Get orders with store products in date range using efficient SQL aggregation
                var orderStats = await _context.Orders
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId))
                    .Select(o => new
                    {
                        OrderId = o.OrderId,
                        CustomerId = o.CustomerId,
                        Date = o.Date,
                        StoreRevenue = o.OrderItems != null ? o.OrderItems
                            .Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                            .Sum(oi => oi.Price * oi.Quantity) : 0,
                        StoreProductsSold = o.OrderItems != null ? o.OrderItems
                            .Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                            .Sum(oi => oi.Quantity) : 0
                    })
                    .ToListAsync(cancellationToken);

                var totalOrders = orderStats.Count;
                var totalRevenue = orderStats.Sum(o => o.StoreRevenue);
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                // Calculate customer metrics
                var customerOrderCounts = orderStats
                    .GroupBy(o => o.CustomerId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var totalCustomers = customerOrderCounts.Count;
                var repeatCustomers = customerOrderCounts.Count(kvp => kvp.Value > 1);
                var customerRetentionRate = totalCustomers > 0 ? (double)repeatCustomers / totalCustomers * 100 : 0;

                // Calculate monthly data
                var monthlyData = orderStats
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new MonthlyOrderData
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Orders = g.Count(),
                        Revenue = g.Sum(o => o.StoreRevenue),
                        NewCustomers = g.Select(o => o.CustomerId).Distinct().Count(), // Simplified
                        ProductsSold = g.Sum(o => o.StoreProductsSold)
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();

                var result = new StoreOrderStats
                {
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue,
                    TotalCustomers = totalCustomers,
                    RepeatCustomers = repeatCustomers,
                    CustomerRetentionRate = customerRetentionRate,
                    MonthlyData = monthlyData
                };

                _logger.LogDebug("Successfully retrieved store order stats for store: {StoreId}. Orders: {TotalOrders}, Revenue: {TotalRevenue}", 
                    storeId, totalOrders, totalRevenue);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get store order stats for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to get store order stats for store {storeId}: {ex.Message}", ex);
            }
        }
    }
}
