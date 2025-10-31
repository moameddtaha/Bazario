using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;
using Bazario.Core.ServiceContracts.Infrastructure;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory alerts and notifications
    /// </summary>
    public class InventoryAlertService : IInventoryAlertService
    {
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;
        private const int DEFAULT_DEAD_STOCK_DAYS = 90;

        private readonly ILogger<InventoryAlertService> _logger;
        private readonly IEmailService _emailService;
        private readonly IInventoryQueryService _inventoryQueryService;
        private readonly IInventoryAnalyticsService _inventoryAnalyticsService;
        private readonly ConcurrentDictionary<Guid, InventoryAlertPreferences> _alertPreferences = new();

        public InventoryAlertService(
            ILogger<InventoryAlertService> logger,
            IEmailService emailService,
            IInventoryQueryService inventoryQueryService,
            IInventoryAnalyticsService inventoryAnalyticsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _inventoryQueryService = inventoryQueryService ?? throw new ArgumentNullException(nameof(inventoryQueryService));
            _inventoryAnalyticsService = inventoryAnalyticsService ?? throw new ArgumentNullException(nameof(inventoryAnalyticsService));
        }

        public async Task SendLowStockAlertAsync(
            Guid productId,
            int currentStock,
            int threshold,
            CancellationToken cancellationToken = default)
        {
            // Input validation
            if (productId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Product ID cannot be empty");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            if (currentStock < 0)
            {
                _logger.LogDebug("Validation failed: Current stock cannot be negative: {CurrentStock}", currentStock);
                throw new ArgumentOutOfRangeException(nameof(currentStock), currentStock, "Current stock cannot be negative");
            }

            if (threshold < 0)
            {
                _logger.LogDebug("Validation failed: Threshold cannot be negative: {Threshold}", threshold);
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold cannot be negative");
            }

            _logger.LogWarning("Low stock alert for product {ProductId}. Current stock: {CurrentStock}, Threshold: {Threshold}",
                productId, currentStock, threshold);

            try
            {
                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = GetAlertPreferences(storeId);
                
                if (!preferences.EnableLowStockAlerts)
                {
                    _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                    return;
                }

                // Send email notification
                var productName = WebUtility.HtmlEncode(inventoryStatus.ProductName ?? "Unknown Product");
                var subject = $"Low Stock Alert: {inventoryStatus.ProductName ?? "Unknown Product"}";
                var body = $@"
                    <h2>Low Stock Alert</h2>
                    <p><strong>Product:</strong> {productName}</p>
                    <p><strong>Current Stock:</strong> {currentStock} units</p>
                    <p><strong>Threshold:</strong> {threshold} units</p>
                    <p><strong>Alert Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>Please consider restocking this product to avoid stockouts.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body);

                if (emailSent)
                {
                    _logger.LogInformation("Low stock alert sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send low stock alert email for product {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send low stock alert for product {ProductId}", productId);
            }
        }

        public async Task SendOutOfStockNotificationAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // Input validation
            if (productId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Product ID cannot be empty");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            _logger.LogCritical("Out of stock notification for product {ProductId}", productId);

            try
            {
                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = GetAlertPreferences(storeId);
                
                if (!preferences.EnableOutOfStockAlerts)
                {
                    _logger.LogDebug("Out of stock alerts disabled for store {StoreId}", storeId);
                    return;
                }

                // Send urgent email notification
                var productName = WebUtility.HtmlEncode(inventoryStatus.ProductName ?? "Unknown Product");
                var subject = $"URGENT: Out of Stock - {inventoryStatus.ProductName ?? "Unknown Product"}";
                var body = $@"
                    <h2 style='color: red;'>OUT OF STOCK ALERT</h2>
                    <p><strong>Product:</strong> {productName}</p>
                    <p><strong>Current Stock:</strong> 0 units</p>
                    <p><strong>Alert Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p style='color: red;'><strong>IMMEDIATE ACTION REQUIRED:</strong> This product is completely out of stock and may be affecting sales.</p>
                    <p>Please restock immediately or temporarily disable the product listing.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body);

                if (emailSent)
                {
                    _logger.LogInformation("Out of stock notification sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send out of stock notification email for product {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send out of stock notification for product {ProductId}", productId);
            }
        }

        public async Task SendBulkLowStockAlertsAsync(
            List<LowStockAlert> alerts,
            CancellationToken cancellationToken = default)
        {
            // Input validation
            if (alerts == null)
            {
                _logger.LogDebug("Validation failed: Alerts list cannot be null");
                throw new ArgumentNullException(nameof(alerts));
            }

            _logger.LogWarning("Sending bulk low stock alerts for {Count} products", alerts.Count);

            if (!alerts.Any())
            {
                _logger.LogDebug("No alerts to send");
                return;
            }

            try
            {
                // Group alerts by store for efficient processing
                var alertsByStore = alerts.GroupBy(a => a.StoreId).ToList();

                // Use direct async lambda instead of Task.Run to avoid thread pool overhead
                var tasks = alertsByStore.Select(async storeGroup =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var storeId = storeGroup.Key;
                    var storeAlerts = storeGroup.ToList();
                    var preferences = GetAlertPreferences(storeId);

                    if (!preferences.EnableLowStockAlerts)
                    {
                        _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                        return;
                    }

                    // Create bulk email content
                    var subject = $"Bulk Low Stock Alert - {storeAlerts.Count} Products";
                    var body = CreateBulkAlertEmailBody(storeAlerts, storeId);

                    var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body);

                    if (emailSent)
                    {
                        _logger.LogInformation("Bulk low stock alert sent for store {StoreId} with {Count} products",
                            storeId, storeAlerts.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send bulk low stock alert email for store {StoreId}", storeId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Bulk low stock alerts sent successfully for {Count} products", alerts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk low stock alerts");
            }
        }

        public async Task SendRestockRecommendationAsync(
            Guid productId,
            int recommendedQuantity,
            string reason,
            CancellationToken cancellationToken = default)
        {
            // Input validation
            if (productId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Product ID cannot be empty");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            if (recommendedQuantity <= 0)
            {
                _logger.LogDebug("Validation failed: Recommended quantity must be positive: {Quantity}", recommendedQuantity);
                throw new ArgumentOutOfRangeException(nameof(recommendedQuantity), recommendedQuantity,
                    "Recommended quantity must be positive");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogDebug("Validation failed: Reason cannot be null or empty");
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
            }

            _logger.LogInformation("Restock recommendation for product {ProductId}: {Quantity} units. Reason: {Reason}",
                productId, recommendedQuantity, reason);

            try
            {
                // Get product details for the recommendation
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = GetAlertPreferences(storeId);
                
                if (!preferences.EnableRestockRecommendations)
                {
                    _logger.LogDebug("Restock recommendations disabled for store {StoreId}", storeId);
                    return;
                }

                // Send restock recommendation email
                var productName = WebUtility.HtmlEncode(inventoryStatus.ProductName ?? "Unknown Product");
                var encodedReason = WebUtility.HtmlEncode(reason);
                var subject = $"Restock Recommendation: {inventoryStatus.ProductName ?? "Unknown Product"}";
                var body = $@"
                    <h2>Restock Recommendation</h2>
                    <p><strong>Product:</strong> {productName}</p>
                    <p><strong>Current Stock:</strong> {inventoryStatus.CurrentStock} units</p>
                    <p><strong>Recommended Quantity:</strong> {recommendedQuantity} units</p>
                    <p><strong>Reason:</strong> {encodedReason}</p>
                    <p><strong>Recommendation Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>This recommendation is based on sales patterns and current inventory levels.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body);

                if (emailSent)
                {
                    _logger.LogInformation("Restock recommendation sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send restock recommendation email for product {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send restock recommendation for product {ProductId}", productId);
            }
        }

        public async Task<int> ProcessPendingAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing pending inventory alerts");

            try
            {
                var processedCount = 0;

                // Get all stores that have alert preferences configured
                var storesWithAlerts = _alertPreferences.Keys.ToList();

                foreach (var storeId in storesWithAlerts)
                {
                    // Use TryGetValue to prevent race condition if preferences are removed
                    if (!_alertPreferences.TryGetValue(storeId, out var preferences))
                    {
                        _logger.LogDebug("Alert preferences removed for store {StoreId} during processing", storeId);
                        continue;
                    }

                    if (!preferences.EnableLowStockAlerts && !preferences.EnableOutOfStockAlerts)
                        continue;

                    // Get low stock products for this store
                    var lowStockProducts = await _inventoryQueryService.GetLowStockAlertsAsync(
                        storeId, 
                        preferences.DefaultLowStockThreshold, 
                        cancellationToken);

                    if (lowStockProducts.Any())
                    {
                        // Send bulk low stock alerts
                        await SendBulkLowStockAlertsAsync(lowStockProducts, cancellationToken);
                        processedCount += lowStockProducts.Count;
                    }

                    // Get out of stock products from the low stock alerts (where IsOutOfStock = true)
                    var outOfStockProducts = lowStockProducts.Where(x => x.IsOutOfStock).ToList();

                    if (outOfStockProducts.Any())
                    {
                        // Send individual out of stock notifications
                        var outOfStockTasks = outOfStockProducts.Select(alert =>
                            SendOutOfStockNotificationAsync(alert.ProductId, cancellationToken));

                        await Task.WhenAll(outOfStockTasks);
                        // Don't add to processedCount - already counted in lowStockProducts above
                    }
                }

                _logger.LogInformation("Processed {Count} pending inventory alerts", processedCount);
                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process pending inventory alerts");
                return 0;
            }
        }

        public Task<bool> ConfigureAlertPreferencesAsync(
            Guid storeId,
            InventoryAlertPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Configuring alert preferences for store {StoreId}", storeId);

            try
            {
                // Validate preferences
                if (preferences == null)
                {
                    _logger.LogError("Alert preferences cannot be null for store {StoreId}", storeId);
                    return Task.FromResult(false);
                }

                if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                {
                    _logger.LogError("Alert email is required for store {StoreId}", storeId);
                    return Task.FromResult(false);
                }

                // Validate email format
                if (!IsValidEmail(preferences.AlertEmail))
                {
                    _logger.LogError("Invalid alert email format for store {StoreId}: {Email}", storeId, preferences.AlertEmail);
                    return Task.FromResult(false);
                }

                // Validate threshold values
                if (preferences.DefaultLowStockThreshold < 0)
                {
                    _logger.LogError("Default low stock threshold cannot be negative for store {StoreId}", storeId);
                    return Task.FromResult(false);
                }

                if (preferences.DeadStockDays <= 0)
                {
                    _logger.LogError("Dead stock days must be positive for store {StoreId}", storeId);
                    return Task.FromResult(false);
                }

                // Set timestamps
                if (preferences.CreatedAt == default)
                {
                    preferences.CreatedAt = DateTime.UtcNow;
                }
                preferences.UpdatedAt = DateTime.UtcNow;

                // Store preferences in memory (in a real app, this would be persisted to database)
                _alertPreferences[storeId] = preferences;

                _logger.LogInformation("Alert preferences configured successfully for store {StoreId}. " +
                    "Low Stock: {LowStock}, Out of Stock: {OutOfStock}, Restock: {Restock}, Threshold: {Threshold}",
                    storeId, 
                    preferences.EnableLowStockAlerts, 
                    preferences.EnableOutOfStockAlerts, 
                    preferences.EnableRestockRecommendations,
                    preferences.DefaultLowStockThreshold);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure alert preferences for store {StoreId}", storeId);
                return Task.FromResult(false);
            }
        }

        private InventoryAlertPreferences GetAlertPreferences(Guid storeId)
        {
            if (_alertPreferences.TryGetValue(storeId, out var preferences))
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
                DefaultLowStockThreshold = DEFAULT_LOW_STOCK_THRESHOLD,
                DeadStockDays = DEFAULT_DEAD_STOCK_DAYS,
                SendDailySummary = false,
                SendWeeklySummary = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string CreateBulkAlertEmailBody(List<LowStockAlert> alerts, Guid storeId)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"
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
                    <tbody>");

            foreach (var alert in alerts)
            {
                var encodedProductName = WebUtility.HtmlEncode(alert.ProductName ?? "Unknown");
                sb.AppendLine($@"
                        <tr>
                            <td>{encodedProductName}</td>
                            <td>{alert.CurrentStock}</td>
                            <td>{alert.Threshold}</td>
                            <td>{alert.AlertDate:yyyy-MM-dd HH:mm}</td>
                        </tr>");
            }

            sb.AppendLine(@"
                    </tbody>
                </table>

                <p>Please review these products and consider restocking to avoid stockouts.</p>");

            return sb.ToString();
        }

    }
}
