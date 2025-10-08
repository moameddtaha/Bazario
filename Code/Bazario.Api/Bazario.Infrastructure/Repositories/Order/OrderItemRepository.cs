using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Order;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Order
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderItemRepository> _logger;

        public OrderItemRepository(ApplicationDbContext context, ILogger<OrderItemRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderItem> AddOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new order item for order: {OrderId}", orderItem?.OrderId);
            
            try
            {
                // Validate input
                if (orderItem == null)
                {
                    _logger.LogWarning("Attempted to add null order item");
                    throw new ArgumentNullException(nameof(orderItem));
                }

                _logger.LogDebug("Adding order item to database context. OrderItemId: {OrderItemId}, OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}, Price: {Price}", 
                    orderItem.OrderItemId, orderItem.OrderId, orderItem.ProductId, orderItem.Quantity, orderItem.Price);

                // Add order item to context
                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added order item. OrderItemId: {OrderItemId}, OrderId: {OrderId}, ProductId: {ProductId}", 
                    orderItem.OrderItemId, orderItem.OrderId, orderItem.ProductId);

                return orderItem;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding order item for order: {OrderId}", orderItem?.OrderId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating order item for order: {OrderId}", orderItem?.OrderId);
                throw new InvalidOperationException($"Unexpected error while creating order item: {ex.Message}", ex);
            }
        }

        public async Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update order item: {OrderItemId}", orderItem?.OrderItemId);
            
            try
            {
                // Validate input
                if (orderItem == null)
                {
                    _logger.LogWarning("Attempted to update null order item");
                    throw new ArgumentNullException(nameof(orderItem));
                }

                if (orderItem.OrderItemId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update order item with empty ID");
                    throw new ArgumentException("Order Item ID cannot be empty", nameof(orderItem));
                }

                _logger.LogDebug("Checking if order item exists in database. OrderItemId: {OrderItemId}", orderItem.OrderItemId);

                // Check if order item exists (use FindAsync for simple PK lookup)
                var existingOrderItem = await _context.OrderItems.FindAsync(new object[] { orderItem.OrderItemId }, cancellationToken);
                if (existingOrderItem == null)
                {
                    _logger.LogWarning("Order item not found for update. OrderItemId: {OrderItemId}", orderItem.OrderItemId);
                    throw new InvalidOperationException($"Order Item with ID {orderItem.OrderItemId} not found");
                }

                _logger.LogDebug("Updating order item properties. OrderItemId: {OrderItemId}, Quantity: {Quantity}, Price: {Price}", 
                    orderItem.OrderItemId, orderItem.Quantity, orderItem.Price);

                // Update only specific properties (not foreign keys or primary key)
                existingOrderItem.Quantity = orderItem.Quantity;
                existingOrderItem.Price = orderItem.Price;
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated order item. OrderItemId: {OrderItemId}, Quantity: {Quantity}, Price: {Price}", 
                    orderItem.OrderItemId, orderItem.Quantity, orderItem.Price);

                return existingOrderItem;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating order item: {OrderItemId}", orderItem?.OrderItemId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating order item: {OrderItemId}", orderItem?.OrderItemId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating order item: {OrderItemId}", orderItem?.OrderItemId);
                throw new InvalidOperationException($"Unexpected error while updating order item with ID {orderItem?.OrderItemId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete order item: {OrderItemId}", orderItemId);
            
            try
            {
                // Validate input
                if (orderItemId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete order item with empty ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Checking if order item exists for deletion. OrderItemId: {OrderItemId}", orderItemId);

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var orderItem = await _context.OrderItems.FindAsync(new object[] { orderItemId }, cancellationToken);
                if (orderItem == null)
                {
                    _logger.LogWarning("Order item not found for deletion. OrderItemId: {OrderItemId}", orderItemId);
                    return false; // Order item not found
                }

                _logger.LogDebug("Removing order item from database context. OrderItemId: {OrderItemId}, OrderId: {OrderId}, ProductId: {ProductId}", 
                    orderItemId, orderItem.OrderId, orderItem.ProductId);

                // Delete the order item
                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted order item. OrderItemId: {OrderItemId}, OrderId: {OrderId}, ProductId: {ProductId}", 
                    orderItemId, orderItem.OrderId, orderItem.ProductId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting order item: {OrderItemId}", orderItemId);
                throw new InvalidOperationException($"Unexpected error while deleting order item with ID {orderItemId}: {ex.Message}", ex);
            }
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve order item by ID: {OrderItemId}", orderItemId);
            
            try
            {
                // Validate input
                if (orderItemId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve order item with empty ID");
                    return null; // Invalid ID
                }

                _logger.LogDebug("Querying order item with navigation properties. OrderItemId: {OrderItemId}", orderItemId);

                // Find the order item with navigation properties
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken);

                if (orderItem == null)
                {
                    _logger.LogDebug("Order item not found. OrderItemId: {OrderItemId}", orderItemId);
                    return null; // Order item not found
                }

                _logger.LogDebug("Successfully retrieved order item. OrderItemId: {OrderItemId}, OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}, Price: {Price}", 
                    orderItem.OrderItemId, orderItem.OrderId, orderItem.ProductId, orderItem.Quantity, orderItem.Price);

                return orderItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order item: {OrderItemId}", orderItemId);
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
                _logger.LogError(ex, "Failed to retrieve all order items");
                throw new InvalidOperationException($"Failed to retrieve order items: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve order items for order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve order items with empty order ID");
                    return new List<OrderItem>(); // Invalid ID, return empty list
                }

                _logger.LogDebug("Querying order items for order with navigation properties. OrderId: {OrderId}", orderId);

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderItemCount} order items for order: {OrderId}", orderItems.Count, orderId);

                return orderItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order items for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to retrieve order items for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve order items for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve order items with empty product ID");
                    return new List<OrderItem>(); // Invalid ID, return empty list
                }

                _logger.LogDebug("Querying order items for product with navigation properties. ProductId: {ProductId}", productId);

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.ProductId == productId)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderItemCount} order items for product: {ProductId}", orderItems.Count, productId);

                return orderItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order items for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to retrieve order items for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderItem>> GetFilteredOrderItemsAsync(Expression<Func<OrderItem, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered order items");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve order items with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Querying filtered order items with navigation properties");

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {OrderItemCount} filtered order items", orderItems.Count);

                return orderItems;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered order items");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered order items");
                throw new InvalidOperationException($"Failed to retrieve filtered order items: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalValueByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate total value for order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to calculate total value with empty order ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Calculating sum of (quantity * price) for order. OrderId: {OrderId}", orderId);

                var totalValue = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .SumAsync(oi => oi.Quantity * oi.Price, cancellationToken);

                _logger.LogDebug("Successfully calculated total value for order {OrderId}: {TotalValue:C}", orderId, totalValue);

                return totalValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate total value for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to calculate total value for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalQuantityByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate total quantity for order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to calculate total quantity with empty order ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Calculating sum of quantities for order. OrderId: {OrderId}", orderId);

                var totalQuantity = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .SumAsync(oi => oi.Quantity, cancellationToken);

                _logger.LogDebug("Successfully calculated total quantity for order {OrderId}: {TotalQuantity}", orderId, totalQuantity);

                return totalQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate total quantity for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to calculate total quantity for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderItemCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count order items for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to count order items with empty product ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Counting order items for product. ProductId: {ProductId}", productId);

                var count = await _context.OrderItems
                    .CountAsync(oi => oi.ProductId == productId, cancellationToken);

                _logger.LogDebug("Successfully counted order items for product {ProductId}: {OrderItemCount}", productId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count order items for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to count order items for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetOrderItemCountByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count order items for order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to count order items with empty order ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Counting order items for order. OrderId: {OrderId}", orderId);

                var count = await _context.OrderItems
                    .CountAsync(oi => oi.OrderId == orderId, cancellationToken);

                _logger.LogDebug("Successfully counted order items for order {OrderId}: {OrderItemCount}", orderId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count order items for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to count order items for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalRevenueByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate total revenue for store: {StoreId}", storeId);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to calculate total revenue with empty store ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Calculating sum of (quantity * price) for store. StoreId: {StoreId}", storeId);

                var totalRevenue = await _context.OrderItems
                    .Where(oi => oi.Product != null && oi.Product.StoreId == storeId)
                    .SumAsync(oi => oi.Quantity * oi.Price, cancellationToken);

                _logger.LogDebug("Successfully calculated total revenue for store {StoreId}: {TotalRevenue:C}", storeId, totalRevenue);

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate total revenue for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to calculate total revenue for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderItemsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete all order items for order: {OrderId}", orderId);
            
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete order items with empty order ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Querying order items for deletion. OrderId: {OrderId}", orderId);

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(cancellationToken);

                if (!orderItems.Any())
                {
                    _logger.LogDebug("No order items found for deletion. OrderId: {OrderId}", orderId);
                    return true; // No items to delete, consider it successful
                }

                _logger.LogDebug("Removing {OrderItemCount} order items from database context. OrderId: {OrderId}", orderItems.Count, orderId);

                _context.OrderItems.RemoveRange(orderItems);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted {OrderItemCount} order items for order: {OrderId}", orderItems.Count, orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete order items for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to delete order items for order {orderId}: {ex.Message}", ex);
            }
        }
    }
}
