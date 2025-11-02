using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using InventoryAlertPreferencesEntity = Bazario.Core.Domain.Entities.Inventory.InventoryAlertPreferences;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Core.ServiceContracts.Infrastructure;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory alerts and notifications
    /// </summary>
    public class InventoryAlertService : IInventoryAlertService, IDisposable
    {
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;
        private const int DEFAULT_DEAD_STOCK_DAYS = 90;
        private const int MAX_BULK_ALERTS = 100;
        private const int MAX_CONCURRENT_STORE_PROCESSING = 10;
        private const int MAX_EMAIL_SUBJECT_LENGTH = 78; // RFC 2822 recommendation
        private const string CACHE_KEY_PREFIX = "AlertPreferences_";
        private static readonly TimeSpan CACHE_SLIDING_EXPIRATION = TimeSpan.FromHours(24);

        private readonly ILogger<InventoryAlertService> _logger;
        private readonly IEmailService _emailService;
        private readonly IInventoryQueryService _inventoryQueryService;
        private readonly IInventoryAnalyticsService _inventoryAnalyticsService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ConcurrentDictionary<Guid, byte> _storeIdsWithPreferences = new();
        private readonly SemaphoreSlim _storeProcessingThrottle = new(MAX_CONCURRENT_STORE_PROCESSING);
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _storePreferencesLocks = new();
        private bool _disposed;

        public InventoryAlertService(
            ILogger<InventoryAlertService> logger,
            IEmailService emailService,
            IInventoryQueryService inventoryQueryService,
            IInventoryAnalyticsService inventoryAnalyticsService,
            IMemoryCache cache,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _inventoryQueryService = inventoryQueryService ?? throw new ArgumentNullException(nameof(inventoryQueryService));
            _inventoryAnalyticsService = inventoryAnalyticsService ?? throw new ArgumentNullException(nameof(inventoryAnalyticsService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<bool> SendLowStockAlertAsync(
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
                var alertTime = DateTime.UtcNow;

                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken).ConfigureAwait(false);
                if (inventoryStatus == null)
                {
                    _logger.LogError("Inventory status not found for product {ProductId}. Cannot send low stock alert.", productId);
                    return false;
                }

                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken).ConfigureAwait(false);
                if (storeId == Guid.Empty)
                {
                    _logger.LogError("Could not determine store for product {ProductId}. Cannot send low stock alert.", productId);
                    return false;
                }

                var preferences = await GetAlertPreferencesAsync(storeId, cancellationToken).ConfigureAwait(false);

                if (!preferences.EnableLowStockAlerts)
                {
                    _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                    return false;
                }

                // Validate alert email is configured
                if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                {
                    _logger.LogError("Alert email not configured for store {StoreId}. Cannot send low stock alert.", storeId);
                    return false;
                }

                // Send email notification
                var productNameRaw = inventoryStatus.ProductName ?? "Unknown Product";
                var productNameEncoded = WebUtility.HtmlEncode(productNameRaw);

                // Email subject is plain text - don't HTML encode, just truncate safely
                var subject = TruncateEmailSubject($"Low Stock Alert: {productNameRaw}");

                var body = $@"
                    <h2>Low Stock Alert</h2>
                    <p><strong>Product:</strong> {productNameEncoded}</p>
                    <p><strong>Current Stock:</strong> {currentStock} units</p>
                    <p><strong>Threshold:</strong> {threshold} units</p>
                    <p><strong>Alert Time:</strong> {alertTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>Please consider restocking this product to avoid stockouts.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body).ConfigureAwait(false);

                if (emailSent)
                {
                    _logger.LogInformation("Low stock alert sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send low stock alert email for product {ProductId}", productId);
                }

                return emailSent;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Low stock alert operation cancelled for product {ProductId}", productId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send low stock alert for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> SendOutOfStockNotificationAsync(
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
                var alertTime = DateTime.UtcNow;

                // Get product details for the alert
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken).ConfigureAwait(false);
                if (inventoryStatus == null)
                {
                    _logger.LogError("Inventory status not found for product {ProductId}. Cannot send out of stock notification.", productId);
                    return false;
                }

                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken).ConfigureAwait(false);
                if (storeId == Guid.Empty)
                {
                    _logger.LogError("Could not determine store for product {ProductId}. Cannot send out of stock notification.", productId);
                    return false;
                }

                var preferences = await GetAlertPreferencesAsync(storeId, cancellationToken).ConfigureAwait(false);

                if (!preferences.EnableOutOfStockAlerts)
                {
                    _logger.LogDebug("Out of stock alerts disabled for store {StoreId}", storeId);
                    return false;
                }

                // Validate alert email is configured
                if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                {
                    _logger.LogError("Alert email not configured for store {StoreId}. Cannot send out of stock notification.", storeId);
                    return false;
                }

                // Send urgent email notification
                var productNameRaw = inventoryStatus.ProductName ?? "Unknown Product";
                var productNameEncoded = WebUtility.HtmlEncode(productNameRaw);

                // Email subject is plain text - don't HTML encode, just truncate safely
                var subject = TruncateEmailSubject($"URGENT: Out of Stock - {productNameRaw}");

                var body = $@"
                    <h2 style='color: red;'>OUT OF STOCK ALERT</h2>
                    <p><strong>Product:</strong> {productNameEncoded}</p>
                    <p><strong>Current Stock:</strong> 0 units</p>
                    <p><strong>Alert Time:</strong> {alertTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p style='color: red;'><strong>IMMEDIATE ACTION REQUIRED:</strong> This product is completely out of stock and may be affecting sales.</p>
                    <p>Please restock immediately or temporarily disable the product listing.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body).ConfigureAwait(false);

                if (emailSent)
                {
                    _logger.LogInformation("Out of stock notification sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send out of stock notification email for product {ProductId}", productId);
                }

                return emailSent;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Out of stock notification operation cancelled for product {ProductId}", productId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send out of stock notification for product {ProductId}", productId);
                return false;
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

            // Validate individual alert objects
            var validAlerts = alerts.Where(a => a != null && a.ProductId != Guid.Empty).ToList();
            if (validAlerts.Count < alerts.Count)
            {
                _logger.LogWarning("Filtered out {Count} invalid alerts", alerts.Count - validAlerts.Count);
            }

            _logger.LogWarning("Sending bulk low stock alerts for {Count} products", validAlerts.Count);

            if (!validAlerts.Any())
            {
                _logger.LogDebug("No valid alerts to send");
                return;
            }

            try
            {
                // Group alerts by store for efficient processing
                var alertsByStore = validAlerts.GroupBy(a => a.StoreId).ToList();

                // Use direct async lambda instead of Task.Run to avoid thread pool overhead
                var tasks = alertsByStore.Select(async storeGroup =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var storeId = storeGroup.Key;
                        var storeAlerts = storeGroup.ToList();
                        var preferences = await GetAlertPreferencesAsync(storeId, cancellationToken).ConfigureAwait(false);

                        if (!preferences.EnableLowStockAlerts)
                        {
                            _logger.LogDebug("Low stock alerts disabled for store {StoreId}", storeId);
                            return;
                        }

                        // Validate alert email is configured
                        if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                        {
                            _logger.LogError("Alert email not configured for store {StoreId}. Cannot send bulk low stock alerts.", storeId);
                            return;
                        }

                        // Create bulk email content with limit
                        var subject = TruncateEmailSubject($"Bulk Low Stock Alert - {storeAlerts.Count} Products");
                        var body = CreateBulkAlertEmailBody(storeAlerts, storeId, cancellationToken);

                        var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body).ConfigureAwait(false);

                        if (emailSent)
                        {
                            _logger.LogInformation("Bulk low stock alert sent for store {StoreId} with {Count} products",
                                storeId, storeAlerts.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to send bulk low stock alert email for store {StoreId}", storeId);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Bulk low stock alert operation cancelled for store {StoreId}", storeGroup.Key);
                        throw;
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Bulk low stock alerts sent successfully for {Count} products", validAlerts.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Bulk low stock alerts operation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk low stock alerts");
            }
        }

        public async Task<bool> SendRestockRecommendationAsync(
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
                var alertTime = DateTime.UtcNow;

                // Get product details for the recommendation
                var inventoryStatus = await _inventoryQueryService.GetInventoryStatusAsync(productId, cancellationToken).ConfigureAwait(false);
                if (inventoryStatus == null)
                {
                    _logger.LogError("Inventory status not found for product {ProductId}. Cannot send restock recommendation.", productId);
                    return false;
                }

                // Get store preferences
                var storeId = await _inventoryAnalyticsService.GetStoreIdForProductAsync(productId, cancellationToken).ConfigureAwait(false);
                if (storeId == Guid.Empty)
                {
                    _logger.LogError("Could not determine store for product {ProductId}. Cannot send restock recommendation.", productId);
                    return false;
                }

                var preferences = await GetAlertPreferencesAsync(storeId, cancellationToken).ConfigureAwait(false);

                if (!preferences.EnableRestockRecommendations)
                {
                    _logger.LogDebug("Restock recommendations disabled for store {StoreId}", storeId);
                    return false;
                }

                // Validate alert email is configured
                if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                {
                    _logger.LogError("Alert email not configured for store {StoreId}. Cannot send restock recommendation.", storeId);
                    return false;
                }

                // Send restock recommendation email
                var productNameRaw = inventoryStatus.ProductName ?? "Unknown Product";
                var productNameEncoded = WebUtility.HtmlEncode(productNameRaw);
                var encodedReason = WebUtility.HtmlEncode(reason);

                // Email subject is plain text - don't HTML encode, just truncate safely
                var subject = TruncateEmailSubject($"Restock Recommendation: {productNameRaw}");

                var body = $@"
                    <h2>Restock Recommendation</h2>
                    <p><strong>Product:</strong> {productNameEncoded}</p>
                    <p><strong>Current Stock:</strong> {inventoryStatus.CurrentStock} units</p>
                    <p><strong>Recommended Quantity:</strong> {recommendedQuantity} units</p>
                    <p><strong>Reason:</strong> {encodedReason}</p>
                    <p><strong>Recommendation Time:</strong> {alertTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>This recommendation is based on sales patterns and current inventory levels.</p>
                ";

                var emailSent = await _emailService.SendGenericAlertEmailAsync(preferences.AlertEmail, subject, body).ConfigureAwait(false);

                if (emailSent)
                {
                    _logger.LogInformation("Restock recommendation sent successfully for product {ProductId}", productId);
                }
                else
                {
                    _logger.LogWarning("Failed to send restock recommendation email for product {ProductId}", productId);
                }

                return emailSent;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Restock recommendation operation cancelled for product {ProductId}", productId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send restock recommendation for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<int?> ProcessPendingAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing pending inventory alerts");

            try
            {
                // Get all stores that have alert preferences configured
                var storesWithAlerts = _storeIdsWithPreferences.Keys.ToList();

                if (storesWithAlerts.Count == 0)
                {
                    _logger.LogDebug("No stores with alert preferences configured");
                    return 0;
                }

                // Process stores in parallel with throttling to prevent overwhelming resources
                var storeTasks = storesWithAlerts.Select(async storeId =>
                {
                    // Use semaphore to limit concurrent store processing (prevents thundering herd)
                    await _storeProcessingThrottle.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var preferences = await GetAlertPreferencesAsync(storeId, cancellationToken).ConfigureAwait(false);

                        if (!preferences.EnableLowStockAlerts && !preferences.EnableOutOfStockAlerts)
                            return 0;

                        // Get low stock products for this store
                        var lowStockProducts = await _inventoryQueryService.GetLowStockAlertsAsync(
                            storeId,
                            preferences.DefaultLowStockThreshold,
                            cancellationToken).ConfigureAwait(false);

                        if (lowStockProducts.Count > 0)
                        {
                            // Send bulk low stock alerts
                            await SendBulkLowStockAlertsAsync(lowStockProducts, cancellationToken).ConfigureAwait(false);

                            // Get out of stock products from the low stock alerts (where IsOutOfStock = true)
                            var outOfStockProducts = lowStockProducts.Where(x => x.IsOutOfStock).ToList();

                            if (outOfStockProducts.Count > 0)
                            {
                                // Send individual out of stock notifications
                                var outOfStockTasks = outOfStockProducts.Select(alert =>
                                    SendOutOfStockNotificationAsync(alert.ProductId, cancellationToken));

                                await Task.WhenAll(outOfStockTasks);
                            }

                            return lowStockProducts.Count;
                        }

                        return 0;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Processing cancelled for store {StoreId}", storeId);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process alerts for store {StoreId}", storeId);
                        return 0;
                    }
                    finally
                    {
                        _storeProcessingThrottle.Release();
                    }
                });

                var results = await Task.WhenAll(storeTasks);
                var processedCount = results.Sum();

                _logger.LogInformation("Processed {Count} pending inventory alerts across {StoreCount} stores",
                    processedCount, storesWithAlerts.Count);
                return processedCount;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Process pending alerts operation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process pending inventory alerts");
                return null; // Return null to indicate error, 0 would mean no alerts processed
            }
        }

        public async Task<bool> ConfigureAlertPreferencesAsync(
            Guid storeId,
            InventoryAlertPreferencesEntity preferences,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Configuring alert preferences for store {StoreId}", storeId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Validate storeId
                if (storeId == Guid.Empty)
                {
                    _logger.LogError("Store ID cannot be empty");
                    return false;
                }

                // Validate preferences
                if (preferences == null)
                {
                    _logger.LogError("Alert preferences cannot be null for store {StoreId}", storeId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(preferences.AlertEmail))
                {
                    _logger.LogError("Alert email is required for store {StoreId}", storeId);
                    return false;
                }

                // Validate email format
                if (!IsValidEmail(preferences.AlertEmail))
                {
                    _logger.LogError("Invalid alert email format for store {StoreId}: {Email}", storeId, preferences.AlertEmail);
                    return false;
                }

                // Validate threshold values
                if (preferences.DefaultLowStockThreshold < 0)
                {
                    _logger.LogError("Default low stock threshold cannot be negative for store {StoreId}", storeId);
                    return false;
                }

                if (preferences.DeadStockDays <= 0)
                {
                    _logger.LogError("Dead stock days must be positive for store {StoreId}", storeId);
                    return false;
                }

                var now = DateTime.UtcNow;

                // Create immutable copy to fix race condition - don't mutate input parameter
                var preferencesToStore = new InventoryAlertPreferencesEntity
                {
                    StoreId = storeId,
                    AlertEmail = preferences.AlertEmail,
                    EnableLowStockAlerts = preferences.EnableLowStockAlerts,
                    EnableOutOfStockAlerts = preferences.EnableOutOfStockAlerts,
                    EnableRestockRecommendations = preferences.EnableRestockRecommendations,
                    EnableDeadStockAlerts = preferences.EnableDeadStockAlerts,
                    DefaultLowStockThreshold = preferences.DefaultLowStockThreshold,
                    DeadStockDays = preferences.DeadStockDays,
                    SendDailySummary = preferences.SendDailySummary,
                    SendWeeklySummary = preferences.SendWeeklySummary,
                    CreatedAt = preferences.CreatedAt == default ? now : preferences.CreatedAt,
                    UpdatedAt = now
                };

                // Invalidate cache BEFORE database write to prevent stale data race condition
                var cacheKey = $"{CACHE_KEY_PREFIX}{storeId}";
                _cache.Remove(cacheKey);

                // Save to database (persistence layer)
                await _unitOfWork.InventoryAlertPreferences.UpsertAsync(preferencesToStore, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                // Track store ID for processing
                _storeIdsWithPreferences.TryAdd(storeId, 0);

                // Cache will be repopulated on next read via GetAlertPreferencesAsync
                // This prevents last-writer-wins race condition

                _logger.LogInformation("Alert preferences configured and persisted successfully for store {StoreId}. " +
                    "Low Stock: {LowStock}, Out of Stock: {OutOfStock}, Restock: {Restock}, Threshold: {Threshold}",
                    storeId,
                    preferences.EnableLowStockAlerts,
                    preferences.EnableOutOfStockAlerts,
                    preferences.EnableRestockRecommendations,
                    preferences.DefaultLowStockThreshold);

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Configure alert preferences operation cancelled for store {StoreId}", storeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure alert preferences for store {StoreId}", storeId);
                return false;
            }
        }

        /// <summary>
        /// Gets alert preferences using cache-aside pattern with double-check locking: cache -> database -> config defaults
        /// Prevents thundering herd problem by using per-store locking on cache miss
        /// </summary>
        private async Task<InventoryAlertPreferencesEntity> GetAlertPreferencesAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{storeId}";

            // Fast path: Try to get from cache without locking
            if (_cache.TryGetValue<InventoryAlertPreferencesEntity>(cacheKey, out var cachedPrefs) && cachedPrefs != null)
            {
                _logger.LogDebug("Retrieved alert preferences from cache for store {StoreId}", storeId);
                return ClonePreferences(cachedPrefs);
            }

            // Slow path: Cache miss - acquire per-store lock to prevent thundering herd
            var lockObj = _storePreferencesLocks.GetOrAdd(storeId, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check cache after acquiring lock (another thread may have populated it)
                if (_cache.TryGetValue<InventoryAlertPreferencesEntity>(cacheKey, out cachedPrefs) && cachedPrefs != null)
                {
                    _logger.LogDebug("Retrieved alert preferences from cache after lock acquisition for store {StoreId}", storeId);
                    return ClonePreferences(cachedPrefs);
                }

                // Layer 2: Try to get from database (persistent storage)
                InventoryAlertPreferencesEntity? preferencesToCache = null;

                try
                {
                    var dbPreferences = await _unitOfWork.InventoryAlertPreferences.GetByStoreIdAsync(storeId, cancellationToken).ConfigureAwait(false);

                    if (dbPreferences != null)
                    {
                        _logger.LogDebug("Retrieved alert preferences from database for store {StoreId}, caching result", storeId);
                        preferencesToCache = dbPreferences;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load preferences from database for store {StoreId}, falling back to config defaults", storeId);
                }

                // Layer 3: Use config defaults if nothing found in database
                if (preferencesToCache == null)
                {
                    _logger.LogDebug("No preferences found for store {StoreId}, using config defaults", storeId);

                    var defaultEmail = _configuration["Alerts:DefaultEmail"];
                    if (string.IsNullOrWhiteSpace(defaultEmail))
                    {
                        throw new InvalidOperationException(
                            "Default alert email is not configured. Please set 'Alerts:DefaultEmail' in configuration (appsettings.json).");
                    }

                    preferencesToCache = new InventoryAlertPreferencesEntity
                    {
                        StoreId = storeId,
                        AlertEmail = defaultEmail,
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

                // Cache the result with eviction callback
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = CACHE_SLIDING_EXPIRATION
                }
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    try
                    {
                        if (value is InventoryAlertPreferencesEntity evictedPrefs)
                        {
                            _storeIdsWithPreferences.TryRemove(evictedPrefs.StoreId, out _);
                            // Avoid logging during shutdown - callback may execute on arbitrary thread
                        }
                    }
                    catch
                    {
                        // Ignore exceptions in callback - cannot safely handle them
                    }
                });

                _cache.Set(cacheKey, preferencesToCache, cacheOptions);
                _storeIdsWithPreferences.TryAdd(storeId, 0);

                return ClonePreferences(preferencesToCache);
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Creates a defensive copy of preferences to prevent external mutation
        /// </summary>
        private static InventoryAlertPreferencesEntity ClonePreferences(InventoryAlertPreferencesEntity source)
        {
            return new InventoryAlertPreferencesEntity
            {
                StoreId = source.StoreId,
                AlertEmail = source.AlertEmail,
                EnableLowStockAlerts = source.EnableLowStockAlerts,
                EnableOutOfStockAlerts = source.EnableOutOfStockAlerts,
                EnableRestockRecommendations = source.EnableRestockRecommendations,
                EnableDeadStockAlerts = source.EnableDeadStockAlerts,
                DefaultLowStockThreshold = source.DefaultLowStockThreshold,
                DeadStockDays = source.DeadStockDays,
                SendDailySummary = source.SendDailySummary,
                SendWeeklySummary = source.SendWeeklySummary,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use regex for faster validation instead of heavyweight MailAddress
                return EmailRegex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Email validation timed out for: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Safely truncates email subject to maximum length without breaking multi-byte UTF-8 characters
        /// </summary>
        private static string TruncateEmailSubject(string subject, int maxLength = MAX_EMAIL_SUBJECT_LENGTH)
        {
            if (subject.Length <= maxLength)
                return subject;

            var textInfo = new StringInfo(subject);
            if (textInfo.LengthInTextElements <= maxLength - 3)
                return subject;

            return textInfo.SubstringByTextElements(0, Math.Min(textInfo.LengthInTextElements, maxLength - 3)) + "...";
        }

        private string CreateBulkAlertEmailBody(List<LowStockAlert> alerts, Guid storeId, CancellationToken cancellationToken)
        {
            var alertTime = DateTime.UtcNow;

            // Limit the number of alerts to prevent huge email bodies
            var alertsToInclude = alerts.Take(MAX_BULK_ALERTS).ToList();
            var hasMore = alerts.Count > MAX_BULK_ALERTS;

            var sb = new StringBuilder();
            sb.AppendLine($@"
                <h2>Bulk Low Stock Alert</h2>
                <p><strong>Store ID:</strong> {storeId}</p>
                <p><strong>Alert Time:</strong> {alertTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>Total Products:</strong> {alerts.Count}</p>");

            if (hasMore)
            {
                sb.AppendLine($@"
                <p><strong>Note:</strong> Showing first {MAX_BULK_ALERTS} of {alerts.Count} alerts. Please review your inventory system for the complete list.</p>");
            }

            sb.AppendLine(@"
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

            foreach (var alert in alertsToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose the store processing throttle semaphore
            _storeProcessingThrottle?.Dispose();

            // Dispose all per-store preference locks
            foreach (var kvp in _storePreferencesLocks)
            {
                kvp.Value?.Dispose();
            }
            _storePreferencesLocks.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
