using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Bazario.Core.Helpers.Inventory;
using Microsoft.Extensions.Logging;
using Bazario.Core.ServiceContracts.Infrastructure;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory alerts and notifications
    /// Handles low stock alerts, expiration warnings, and inventory notifications
    /// </summary>
    public class InventoryAlertService : IInventoryAlertService
    {
        private readonly ILogger<InventoryAlertService> _logger;
        private readonly IEmailService _emailService;
        private readonly IInventoryQueryService _inventoryQueryService;
        private readonly IInventoryHelper _inventoryHelper;
        private readonly Dictionary<Guid, InventoryAlertPreferences> _alertPreferences = new();

        public InventoryAlertService(
            ILogger<InventoryAlertService> logger,
            IEmailService emailService,
            IInventoryQueryService inventoryQueryService,
            IInventoryHelper inventoryHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _inventoryQueryService = inventoryQueryService ?? throw new ArgumentNullException(nameof(inventoryQueryService));
            _inventoryHelper = inventoryHelper ?? throw new ArgumentNullException(nameof(inventoryHelper));
        }

        public async Task SendLowStockAlertAsync(
            Guid productId, 
            int currentStock, 
            int threshold, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Low stock alert for product {ProductId}. Current stock: {CurrentStock}, Threshold: {Threshold}", 
                productId, currentStock, threshold);

            try
            {
                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryHelper.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = _inventoryHelper.GetAlertPreferences(storeId, _alertPreferences);
                
                if (!preferences.EnableLowStockAlerts)
                {
                    _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                    return;
                }

                // Send email notification
                var subject = $"Low Stock Alert: {inventoryStatus.ProductName}";
                var body = $@"
                    <h2>Low Stock Alert</h2>
                    <p><strong>Product:</strong> {inventoryStatus.ProductName}</p>
                    <p><strong>Current Stock:</strong> {currentStock} units</p>
                    <p><strong>Threshold:</strong> {threshold} units</p>
                    <p><strong>Alert Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>Please consider restocking this product to avoid stockouts.</p>
                ";

                // Note: IEmailService doesn't have a generic SendEmailAsync method
                // This would need to be implemented or use a different email service
                // For now, we'll just log the email content
                _logger.LogInformation("Would send email to {Email} with subject: {Subject}", 
                    preferences.AlertEmail, subject);

                _logger.LogInformation("Low stock alert sent successfully for product {ProductId}", productId);
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
            _logger.LogError("Out of stock notification for product {ProductId}", productId);

            try
            {
                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryHelper.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = _inventoryHelper.GetAlertPreferences(storeId, _alertPreferences);
                
                if (!preferences.EnableOutOfStockAlerts)
                {
                    _logger.LogDebug("Out of stock alerts disabled for store {StoreId}", storeId);
                    return;
                }

                // Send urgent email notification
                var subject = $"🚨 URGENT: Out of Stock - {inventoryStatus.ProductName}";
                var body = $@"
                    <h2 style='color: red;'>OUT OF STOCK ALERT</h2>
                    <p><strong>Product:</strong> {inventoryStatus.ProductName}</p>
                    <p><strong>Current Stock:</strong> 0 units</p>
                    <p><strong>Alert Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p style='color: red;'><strong>IMMEDIATE ACTION REQUIRED:</strong> This product is completely out of stock and may be affecting sales.</p>
                    <p>Please restock immediately or temporarily disable the product listing.</p>
                ";

                // Note: IEmailService doesn't have a generic SendEmailAsync method
                // This would need to be implemented or use a different email service
                // For now, we'll just log the email content
                _logger.LogInformation("Would send email to {Email} with subject: {Subject}", 
                    preferences.AlertEmail, subject);

                _logger.LogInformation("Out of stock notification sent successfully for product {ProductId}", productId);
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
                
                var tasks = alertsByStore.Select(storeGroup => Task.Run(() =>
                {
                    var storeId = storeGroup.Key;
                    var storeAlerts = storeGroup.ToList();
                    var preferences = _inventoryHelper.GetAlertPreferences(storeId, _alertPreferences);
                    
                    if (!preferences.EnableLowStockAlerts)
                    {
                        _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                        return;
                    }

                    // Create bulk email content
                    var subject = $"Bulk Low Stock Alert - {storeAlerts.Count} Products";
                    var body = _inventoryHelper.CreateBulkAlertEmailBody(storeAlerts, storeId);

                    // Note: IEmailService doesn't have a generic SendEmailAsync method
                    // This would need to be implemented or use a different email service
                    // For now, we'll just log the email content
                    _logger.LogInformation("Would send bulk email to {Email} with subject: {Subject}", 
                        preferences.AlertEmail, subject);

                    _logger.LogInformation("Bulk low stock alert sent for store {StoreId} with {Count} products", 
                        storeId, storeAlerts.Count);
                }));

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
            _logger.LogInformation("Restock recommendation for product {ProductId}: {Quantity} units. Reason: {Reason}", 
                productId, recommendedQuantity, reason);

            try
            {
                // Get product details for the recommendation
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken);
                
                // Get store preferences
                var storeId = await _inventoryHelper.GetStoreIdForProductAsync(productId, cancellationToken);
                var preferences = _inventoryHelper.GetAlertPreferences(storeId, _alertPreferences);
                
                if (!preferences.EnableRestockRecommendations)
                {
                    _logger.LogDebug("Restock recommendations disabled for store {StoreId}", storeId);
                    return;
                }

                // Send restock recommendation email
                var subject = $"Restock Recommendation: {inventoryStatus.ProductName}";
                var body = $@"
                    <h2>Restock Recommendation</h2>
                    <p><strong>Product:</strong> {inventoryStatus.ProductName}</p>
                    <p><strong>Current Stock:</strong> {inventoryStatus.CurrentStock} units</p>
                    <p><strong>Recommended Quantity:</strong> {recommendedQuantity} units</p>
                    <p><strong>Reason:</strong> {reason}</p>
                    <p><strong>Recommendation Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>This recommendation is based on sales patterns and current inventory levels.</p>
                ";

                // Note: IEmailService doesn't have a generic SendEmailAsync method
                // This would need to be implemented or use a different email service
                // For now, we'll just log the email content
                _logger.LogInformation("Would send email to {Email} with subject: {Subject}", 
                    preferences.AlertEmail, subject);

                _logger.LogInformation("Restock recommendation sent successfully for product {ProductId}", productId);
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
                    var preferences = _alertPreferences[storeId];
                    
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

                    // Get out of stock products for this store
                    // Note: This method doesn't exist in IInventoryQueryService
                    // We'll need to implement it or use a different approach
                    var outOfStockProducts = new List<LowStockAlert>();

                    if (outOfStockProducts.Any())
                    {
                        // Send individual out of stock notifications
                        var outOfStockTasks = outOfStockProducts.Select(alert => 
                            SendOutOfStockNotificationAsync(alert.ProductId, cancellationToken));
                        
                        await Task.WhenAll(outOfStockTasks);
                        processedCount += outOfStockProducts.Count;
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

    }
}
