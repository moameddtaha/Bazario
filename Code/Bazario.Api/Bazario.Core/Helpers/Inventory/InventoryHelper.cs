using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Inventory;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Inventory
{
    /// <summary>
    /// Helper class for inventory-related operations
    /// </summary>
    public class InventoryHelper : IInventoryHelper
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<InventoryHelper> _logger;

        public InventoryHelper(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            ILogger<InventoryHelper> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Guid> GetStoreIdForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query the database to get the store ID for the product
                var productEntity = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (productEntity != null)
                {
                    return productEntity.StoreId;
                }

                _logger.LogWarning("Product {ProductId} not found when getting store ID", productId);
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store ID for product {ProductId}", productId);
                return Guid.Empty;
            }
        }

        public async Task<int> GetProductSalesQuantityAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query order items to get sales quantity for the product
                var orders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
                var ordersInRange = orders.Where(o => o.Date >= startDate && o.Date <= endDate).ToList();
                
                var totalQuantity = 0;
                foreach (var order in ordersInRange)
                {
                    // Get order items for this order and product
                    if (order.OrderItems != null)
                    {
                        var productItems = order.OrderItems.Where(oi => oi.ProductId == productId);
                        totalQuantity += productItems.Sum(oi => oi.Quantity);
                    }
                }

                return totalQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales quantity for product {ProductId}", productId);
                return 0;
            }
        }

        public async Task<decimal> GetProductRevenueAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query order items to get revenue for the product
                var orders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
                var ordersInRange = orders.Where(o => o.Date >= startDate && o.Date <= endDate).ToList();
                
                var totalRevenue = 0m;
                foreach (var order in ordersInRange)
                {
                    // Get order items for this order and product
                    if (order.OrderItems != null)
                    {
                        var productItems = order.OrderItems.Where(oi => oi.ProductId == productId);
                        totalRevenue += productItems.Sum(oi => oi.Price * oi.Quantity);
                    }
                }

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue for product {ProductId}", productId);
                return 0;
            }
        }

        public InventoryAlertPreferences GetAlertPreferences(Guid storeId, Dictionary<Guid, InventoryAlertPreferences> alertPreferences)
        {
            if (alertPreferences.TryGetValue(storeId, out var preferences))
            {
                return preferences;
            }

            // Return default preferences if none configured
            return new InventoryAlertPreferences
            {
                StoreId = storeId,
                AlertEmail = "admin@bazario.com", // Default email
                EnableLowStockAlerts = true,
                EnableOutOfStockAlerts = true,
                EnableRestockRecommendations = true,
                DefaultLowStockThreshold = 10, // Default threshold
                SendDailySummary = false,
                SendWeeklySummary = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public string CreateBulkAlertEmailBody(List<LowStockAlert> alerts, Guid storeId)
        {
            var alertRows = alerts.Select(alert => $@"
                <tr>
                    <td>{alert.ProductName}</td>
                    <td>{alert.CurrentStock}</td>
                    <td>{alert.Threshold}</td>
                    <td>{alert.AlertDate:yyyy-MM-dd HH:mm}</td>
                </tr>").ToList();

            return $@"
                <h2>Bulk Low Stock Alert</h2>
                <p><strong>Store ID:</strong> {storeId}</p>
                <p><strong>Alert Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>Total Products:</strong> {alerts.Count}</p>
                
                <table border='1' style='border-collapse: collapse; width: 100%;'>
                    <thead>
                        <tr style='background-color: #f2f2f2;'>
                            <th>Product Name</th>
                            <th>Current Stock</th>
                            <th>Threshold</th>
                            <th>Alert Date</th>
                        </tr>
                    </thead>
                    <tbody>
                        {string.Join("", alertRows)}
                    </tbody>
                </table>
                
                <p>Please review these products and consider restocking to avoid stockouts.</p>
            ";
        }
    }
}
