using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Store;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonthlyOrderData = Bazario.Core.Models.Store.MonthlyOrderData;

namespace Bazario.Infrastructure.Repositories.Order
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

        public Task<OrderEntity> AddOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
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

                _logger.LogInformation("Successfully added order. OrderId: {OrderId}, Total: {OrderTotal}",
                    order.OrderId, order.TotalAmount);

                return Task.FromResult(order);
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

        public async Task<OrderEntity> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
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

                _logger.LogDebug("Updating order properties. OrderId: {OrderId}, Date: {Date}, TotalAmount: {TotalAmount}, Status: {Status}, Subtotal: {Subtotal}, DiscountAmount: {DiscountAmount}, ShippingCost: {ShippingCost}", 
                    order.OrderId, order.Date, order.TotalAmount, order.Status, order.Subtotal, order.DiscountAmount, order.ShippingCost);

                // Update basic order properties
                existingOrder.Date = order.Date;
                
                if (!string.IsNullOrEmpty(order.Status))
                {
                    existingOrder.Status = order.Status;
                }

                // Update financial properties (only if they have valid values)
                if (order.Subtotal >= 0)
                {
                    existingOrder.Subtotal = order.Subtotal;
                }
                
                if (order.DiscountAmount >= 0)
                {
                    existingOrder.DiscountAmount = order.DiscountAmount;
                }
                
                if (order.ShippingCost >= 0)
                {
                    existingOrder.ShippingCost = order.ShippingCost;
                }
                
                if (order.TotalAmount >= 0)
                {
                    existingOrder.TotalAmount = order.TotalAmount;
                }

                // Update discount-related properties (only if not null/empty)
                if (!string.IsNullOrEmpty(order.AppliedDiscountCodes))
                {
                    existingOrder.AppliedDiscountCodes = order.AppliedDiscountCodes;
                }
                
                if (!string.IsNullOrEmpty(order.AppliedDiscountTypes))
                {
                    existingOrder.AppliedDiscountTypes = order.AppliedDiscountTypes;
                }
                

                _logger.LogInformation("Successfully updated order. OrderId: {OrderId}, Status: {Status}, Subtotal: {Subtotal}, DiscountAmount: {DiscountAmount}, ShippingCost: {ShippingCost}, TotalAmount: {TotalAmount}", 
                    order.OrderId, order.Status, order.Subtotal, order.DiscountAmount, order.ShippingCost, order.TotalAmount);

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

        public async Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting soft delete for order: {OrderId}, DeletedBy: {DeletedBy}, Reason: {Reason}", orderId, deletedBy, reason);

            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete order with empty ID");
                    return false;
                }

                if (deletedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete order without valid DeletedBy user ID");
                    return false;
                }

                _logger.LogDebug("Checking if order exists for soft deletion. OrderId: {OrderId}", orderId);

                // Find the order (should be active, not already deleted)
                var order = await _context.Orders
                    .IgnoreQueryFilters() // This allows finding deleted orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for soft deletion. OrderId: {OrderId}", orderId);
                    return false;
                }

                if (order.IsDeleted)
                {
                    _logger.LogWarning("Order is already soft deleted. OrderId: {OrderId}", orderId);
                    return false;
                }

                _logger.LogDebug("Soft deleting order. OrderId: {OrderId}", orderId);

                // Set soft delete properties
                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow;
                order.DeletedBy = deletedBy;
                order.DeletedReason = reason;

                // No need to call Update() - entity is already tracked by EF Core

                _logger.LogInformation("Successfully soft deleted order. OrderId: {OrderId}, DeletedBy: {DeletedBy}",
                    orderId, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while soft deleting order: {OrderId}", orderId);
                throw new InvalidOperationException($"Unexpected error while soft deleting order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> RestoreOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting restore for soft deleted order: {OrderId}", orderId);

            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to restore order with empty ID");
                    return false;
                }

                _logger.LogDebug("Checking if soft deleted order exists for restore. OrderId: {OrderId}", orderId);

                // Find the order including soft deleted ones
                var order = await _context.Orders
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for restore. OrderId: {OrderId}", orderId);
                    return false;
                }

                if (!order.IsDeleted)
                {
                    _logger.LogWarning("Order is not soft deleted, cannot restore. OrderId: {OrderId}", orderId);
                    return false;
                }

                _logger.LogDebug("Restoring soft deleted order. OrderId: {OrderId}", orderId);

                // Clear soft delete properties
                order.IsDeleted = false;
                order.DeletedAt = null;
                order.DeletedBy = null;
                order.DeletedReason = null;

                // No need to call Update() - entity is already tracked by EF Core

                _logger.LogInformation("Successfully restored order. OrderId: {OrderId}", orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while restoring order: {OrderId}", orderId);
                throw new InvalidOperationException($"Unexpected error while restoring order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<OrderEntity?> GetOrderByIdIncludeDeletedAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve order with empty ID");
                    return null;
                }

                _logger.LogDebug("Retrieving order including soft deleted. OrderId: {OrderId}", orderId);

                // Query with navigation properties, ignoring soft delete filter
                var order = await _context.Orders
                    .IgnoreQueryFilters()
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order != null)
                {
                    _logger.LogDebug("Successfully retrieved order including deleted. OrderId: {OrderId}, IsDeleted: {IsDeleted}",
                        orderId, order.IsDeleted);
                }
                else
                {
                    _logger.LogDebug("Order not found including deleted. OrderId: {OrderId}", orderId);
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order including deleted: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to retrieve order with ID {orderId}: {ex.Message}", ex);
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

                // Use IgnoreQueryFilters to find both active and soft-deleted orders
                var order = await _context.Orders
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for deletion. OrderId: {OrderId}", orderId);
                    return false; // Order not found
                }

                _logger.LogDebug("Removing order from database context. OrderId: {OrderId}", orderId);

                // Delete the order
                _context.Orders.Remove(order);

                _logger.LogInformation("Successfully deleted order. OrderId: {OrderId}", orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting order: {OrderId}", orderId);
                throw new InvalidOperationException($"Unexpected error while deleting order with ID {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<OrderEntity?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
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

        public async Task<List<OrderEntity>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
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

        public async Task<List<OrderEntity>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve orders with empty customer ID");
                    return new List<OrderEntity>(); // Invalid ID, return empty list
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

        public async Task<List<OrderEntity>> GetOrdersByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve orders with empty store ID");
                    return new List<OrderEntity>(); // Invalid ID, return empty list
                }

                // Get orders by joining through OrderItems -> Product -> Store
                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId))
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {OrderCount} orders for store {StoreId}", orders.Count, storeId);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to retrieve orders for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderEntity>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
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

        public async Task<List<OrderEntity>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
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

        public async Task<List<OrderEntity>> GetFilteredOrdersAsync(Expression<Func<OrderEntity, bool>> predicate, CancellationToken cancellationToken = default)
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
                    .AsNoTracking()
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId))
                    .Select(o => new OrderStatsProjection
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

                // Identify customers with orders before the range to avoid miscounting returning customers as new
                var priorCustomerIds = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Date < startDate)
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId))
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var priorCustomers = new HashSet<Guid>(priorCustomerIds);

                // Calculate monthly data with proper new customer calculation
                var customerFirstOrderDates = orderStats
                    .GroupBy(o => o.CustomerId)
                    .ToDictionary(g => g.Key, g => g.Min(o => o.Date));

                var monthlyData = orderStats
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new MonthlyOrderData
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Orders = g.Count(),
                        Revenue = g.Sum(o => o.StoreRevenue),
                        NewCustomers = g.Select(o => o.CustomerId).Distinct()
                                        .Count(customerId => !priorCustomers.Contains(customerId) &&
                                               customerFirstOrderDates[customerId].Year == g.Key.Year && 
                                               customerFirstOrderDates[customerId].Month == g.Key.Month),
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

        public async Task<Dictionary<Guid, StoreOrderStats>> GetBulkStoreOrderStatsAsync(List<Guid> storeIds, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting bulk store order stats for {StoreCount} stores from {StartDate} to {EndDate}", 
                storeIds.Count, startDate, endDate);

            try
            {
                if (!storeIds.Any())
                {
                    return new Dictionary<Guid, StoreOrderStats>();
                }

                // Get all orders for the specified stores and date range in a single query
                // Orders are related to stores through OrderItems -> Product -> Store
                var orders = await _context.Orders
                    .Where(o => o.OrderItems != null && 
                               o.OrderItems.Any(oi => oi.Product != null && storeIds.Contains(oi.Product.StoreId)) &&
                               o.Date >= startDate && 
                               o.Date <= endDate)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Group by store and calculate stats
                var result = new Dictionary<Guid, StoreOrderStats>();

                foreach (var storeId in storeIds)
                {
                    // Filter orders that have items from this specific store
                    var storeOrders = orders.Where(o => o.OrderItems != null && 
                                                      o.OrderItems.Any(oi => oi.Product != null && oi.Product.StoreId == storeId)).ToList();
                    
                    if (!storeOrders.Any())
                    {
                        result[storeId] = new StoreOrderStats
                        {
                            TotalOrders = 0,
                            TotalRevenue = 0,
                            AverageOrderValue = 0,
                            TotalCustomers = 0,
                            RepeatCustomers = 0,
                            CustomerRetentionRate = 0,
                            MonthlyData = new List<MonthlyOrderData>()
                        };
                        continue;
                    }

                    // Calculate revenue for this store (only from items belonging to this store)
                    var totalRevenue = storeOrders.Sum(o => 
                        o.OrderItems?.Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                         .Sum(oi => oi.Price * oi.Quantity) ?? 0);
                    
                    var totalOrders = storeOrders.Count;
                    var uniqueCustomers = storeOrders.Select(o => o.CustomerId).Distinct().ToList();
                    var totalCustomers = uniqueCustomers.Count;

                    // Calculate repeat customers
                    var customerOrderCounts = storeOrders
                        .GroupBy(o => o.CustomerId)
                        .ToDictionary(g => g.Key, g => g.Count());
                    
                    var repeatCustomers = customerOrderCounts.Count(kvp => kvp.Value > 1);
                    var customerRetentionRate = totalCustomers > 0 ? (double)repeatCustomers / totalCustomers * 100 : 0;

                    // Calculate monthly data
                    var monthlyData = storeOrders
                        .GroupBy(o => new { o.Date.Year, o.Date.Month })
                        .Select(g => new MonthlyOrderData
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Orders = g.Count(),
                            Revenue = g.Sum(o => o.OrderItems?.Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                                                      .Sum(oi => oi.Price * oi.Quantity) ?? 0),
                            NewCustomers = 0, // Would need additional logic to determine new vs returning customers
                            ProductsSold = g.Sum(o => o.OrderItems?.Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                                                           .Sum(oi => oi.Quantity) ?? 0)
                        })
                        .OrderBy(m => m.Year).ThenBy(m => m.Month)
                        .ToList();

                    result[storeId] = new StoreOrderStats
                    {
                        TotalOrders = totalOrders,
                        TotalRevenue = totalRevenue,
                        AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
                        TotalCustomers = totalCustomers,
                        RepeatCustomers = repeatCustomers,
                        CustomerRetentionRate = customerRetentionRate,
                        MonthlyData = monthlyData
                    };
                }

                _logger.LogDebug("Successfully calculated bulk store order stats for {StoreCount} stores", storeIds.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bulk store order stats for {StoreCount} stores", storeIds.Count);
                throw new InvalidOperationException($"Failed to get bulk store order stats: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderEntity>> GetOrdersByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting orders by discount code: {DiscountCode}", discountCode);

            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    _logger.LogWarning("Discount code is null or empty");
                    return new List<OrderEntity>();
                }

                var orders = await _context.Orders
                    .Where(o => o.AppliedDiscountCodes != null && o.AppliedDiscountCodes.Contains(discountCode))
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {OrderCount} orders with discount code: {DiscountCode}", orders.Count, discountCode);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders by discount code: {DiscountCode}", discountCode);
                throw new InvalidOperationException($"Failed to get orders by discount code: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderWithCodeCount>> GetOrdersWithCodeCountsByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting orders with code counts by discount code: {DiscountCode}", discountCode);

            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    _logger.LogWarning("Discount code is null or empty");
                    return new List<OrderWithCodeCount>();
                }

                var orders = await _context.Orders
                    .Where(o => o.AppliedDiscountCodes != null && o.AppliedDiscountCodes.Contains(discountCode))
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Pre-calculate code counts and proportional amounts at repository level
                var ordersWithCodeCounts = orders.Select(o => {
                    var codes = (o.AppliedDiscountCodes ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    
                    var codeCount = codes.Count;
                    return new OrderWithCodeCount
                    {
                        Order = o,
                        CodeCount = codeCount,
                        ProportionalDiscountAmount = codeCount > 0 ? o.DiscountAmount / codeCount : 0,
                        ProportionalTotalAmount = codeCount > 0 ? o.TotalAmount / codeCount : 0
                    };
                }).ToList();

                _logger.LogDebug("Found {OrderCount} orders with pre-calculated code counts for discount code: {DiscountCode}", 
                    ordersWithCodeCounts.Count, discountCode);

                return ordersWithCodeCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders with code counts by discount code: {DiscountCode}", discountCode);
                throw new InvalidOperationException($"Failed to get orders with code counts by discount code: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderEntity>> GetOrdersByDiscountCodeAndDateRangeAsync(string discountCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting orders by discount code: {DiscountCode} between {StartDate} and {EndDate}", discountCode, startDate, endDate);

            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    _logger.LogWarning("Discount code is null or empty");
                    return new List<OrderEntity>();
                }

                var orders = await _context.Orders
                    .Where(o => o.Date >= startDate && o.Date <= endDate &&
                                o.AppliedDiscountCodes != null && o.AppliedDiscountCodes.Contains(discountCode))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {OrderCount} orders for discount code: {DiscountCode} in date range", orders.Count, discountCode);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders by discount code and date range: {DiscountCode}", discountCode);
                throw new InvalidOperationException($"Failed to get orders by discount code and date range: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderEntity>> GetOrdersWithDiscountsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting all orders with discounts between {StartDate} and {EndDate}", startDate, endDate);

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.Date >= startDate && o.Date <= endDate && o.DiscountAmount > 0)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {OrderCount} orders with discounts in date range", orders.Count);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders with discounts");
                throw new InvalidOperationException($"Failed to get orders with discounts: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderCountByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting order count by discount code: {DiscountCode}", discountCode);

            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    _logger.LogWarning("Discount code is null or empty");
                    return 0;
                }

                var count = await _context.Orders
                    .CountAsync(o => o.AppliedDiscountCodes != null && o.AppliedDiscountCodes.Contains(discountCode), cancellationToken);

                _logger.LogDebug("Found {OrderCount} orders for discount code: {DiscountCode}", count, discountCode);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order count by discount code: {DiscountCode}", discountCode);
                throw new InvalidOperationException($"Failed to get order count by discount code: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrdersCountByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting order count between {StartDate} and {EndDate}", startDate, endDate);

            try
            {
                var count = await _context.Orders
                    .CountAsync(o => o.Date >= startDate && o.Date <= endDate, cancellationToken);

                _logger.LogDebug("Found {OrderCount} orders in date range", count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order count by date range");
                throw new InvalidOperationException($"Failed to get order count by date range: {ex.Message}", ex);
            }
        }
    }
}
