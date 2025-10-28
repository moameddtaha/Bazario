using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory validation and business rules
    /// Handles stock availability checks and inventory constraints
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class InventoryValidationService : IInventoryValidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryValidationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<Guid, int> _productThresholds = new();
        private readonly ConcurrentDictionary<Guid, int> _storeThresholds = new();

        // Constants
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;
        private const string CONFIG_KEY_DEFAULT_THRESHOLD = "Inventory:DefaultLowStockThreshold";

        public InventoryValidationService(
            IUnitOfWork unitOfWork,
            ILogger<InventoryValidationService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Validates stock availability for multiple products
        /// </summary>
        /// <param name="stockCheckRequest">List of products and quantities to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of validation results for each product</returns>
        public async Task<List<StockValidationResult>> ValidateStockAvailabilityAsync(
            List<StockCheckItem> stockCheckRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (stockCheckRequest == null)
                {
                    throw new ArgumentNullException(nameof(stockCheckRequest), "Stock check request cannot be null");
                }

                if (stockCheckRequest.Count == 0)
                {
                    throw new ArgumentException("Stock check request cannot be empty", nameof(stockCheckRequest));
                }

                _logger.LogDebug("Validating stock availability for {Count} items", stockCheckRequest.Count);

                var results = new List<StockValidationResult>();

                foreach (var item in stockCheckRequest)
                {
                    // Validate item
                    if (item.ProductId == Guid.Empty)
                    {
                        throw new ArgumentException($"Product ID cannot be empty at index {stockCheckRequest.IndexOf(item)}", nameof(stockCheckRequest));
                    }

                    if (item.RequestedQuantity <= 0)
                    {
                        throw new ArgumentException($"Requested quantity must be greater than 0 at index {stockCheckRequest.IndexOf(item)}", nameof(stockCheckRequest));
                    }

                    var product = await _unitOfWork.Products.GetProductByIdAsync(item.ProductId, cancellationToken);

                    var result = new StockValidationResult
                    {
                        ProductId = item.ProductId,
                        RequestedQuantity = item.RequestedQuantity,
                        AvailableQuantity = product?.StockQuantity ?? 0,
                        IsAvailable = IsProductAvailableForQuantity(product, item.RequestedQuantity),
                        Message = GetValidationMessage(product, item.RequestedQuantity)
                    };

                    results.Add(result);
                }

                _logger.LogDebug("Stock validation completed. {AvailableCount} of {TotalCount} items available",
                    results.Count(r => r.IsAvailable), results.Count);

                return results;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate stock availability");
                throw new InvalidOperationException($"Failed to validate stock availability: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates if a stock update is allowed
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="newQuantity">New stock quantity</param>
        /// <param name="updateType">Type of stock update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateStockUpdateAsync(
            Guid productId,
            int newQuantity,
            StockUpdateType updateType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                _logger.LogDebug("Validating stock update for product {ProductId}", productId);

                if (newQuantity < 0)
                {
                    _logger.LogWarning("Invalid quantity {Quantity} for product {ProductId}", newQuantity, productId);
                    return false;
                }

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for validation", productId);
                    return false;
                }

                // Validate based on update type
                if ((updateType == StockUpdateType.Sale || updateType == StockUpdateType.Damage) && product.StockQuantity < newQuantity)
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Current: {Current}, Requested: {Requested}",
                        productId, product.StockQuantity, newQuantity);
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate stock update for product {ProductId}", productId);
                throw new InvalidOperationException($"Failed to validate stock update: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates if a reservation can be created
        /// </summary>
        /// <param name="reservationRequest">Reservation request details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateReservationAsync(
            StockReservationRequest reservationRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (reservationRequest == null)
                {
                    throw new ArgumentNullException(nameof(reservationRequest), "Reservation request cannot be null");
                }

                if (reservationRequest.Items == null || reservationRequest.Items.Count == 0)
                {
                    throw new ArgumentException("Reservation request must contain at least one item", nameof(reservationRequest));
                }

                _logger.LogDebug("Validating reservation request for {Count} items", reservationRequest.Items.Count);

                foreach (var item in reservationRequest.Items)
                {
                    if (item.ProductId == Guid.Empty)
                    {
                        throw new ArgumentException("Product ID cannot be empty in reservation items", nameof(reservationRequest));
                    }

                    if (item.Quantity <= 0)
                    {
                        throw new ArgumentException("Quantity must be greater than 0 in reservation items", nameof(reservationRequest));
                    }

                    var product = await _unitOfWork.Products.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (!IsProductAvailableForQuantity(product, item.Quantity))
                    {
                        _logger.LogWarning("Reservation validation failed for product {ProductId}", item.ProductId);
                        return false;
                    }
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate reservation");
                throw new InvalidOperationException($"Failed to validate reservation: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if product has sufficient stock for quantity
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="requiredQuantity">Required quantity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if sufficient stock, false otherwise</returns>
        public async Task<bool> HasSufficientStockAsync(
            Guid productId,
            int requiredQuantity,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (requiredQuantity <= 0)
                {
                    throw new ArgumentException("Required quantity must be greater than 0", nameof(requiredQuantity));
                }

                _logger.LogDebug("Checking if product {ProductId} has sufficient stock for {Quantity} units",
                    productId, requiredQuantity);

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                return IsProductAvailableForQuantity(product, requiredQuantity);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check stock availability for product {ProductId}", productId);
                throw new InvalidOperationException($"Failed to check stock availability: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates bulk stock update data
        /// </summary>
        /// <param name="bulkUpdateRequest">Bulk update request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of validation errors</returns>
        public async Task<List<BulkUpdateError>> ValidateBulkUpdateAsync(
            BulkStockUpdateRequest bulkUpdateRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (bulkUpdateRequest == null)
                {
                    throw new ArgumentNullException(nameof(bulkUpdateRequest), "Bulk update request cannot be null");
                }

                if (bulkUpdateRequest.Items == null || bulkUpdateRequest.Items.Count == 0)
                {
                    throw new ArgumentException("Bulk update request must contain at least one item", nameof(bulkUpdateRequest));
                }

                _logger.LogDebug("Validating bulk update request for {Count} items", bulkUpdateRequest.Items.Count);

                var errors = new List<BulkUpdateError>();

                foreach (var item in bulkUpdateRequest.Items)
                {
                    if (item.ProductId == Guid.Empty)
                    {
                        errors.Add(new BulkUpdateError
                        {
                            ProductId = item.ProductId,
                            ErrorMessage = "Product ID cannot be empty"
                        });
                        continue;
                    }

                    if (item.NewQuantity < 0)
                    {
                        errors.Add(new BulkUpdateError
                        {
                            ProductId = item.ProductId,
                            ErrorMessage = "Quantity cannot be negative"
                        });
                        continue;
                    }

                    var product = await _unitOfWork.Products.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        errors.Add(new BulkUpdateError
                        {
                            ProductId = item.ProductId,
                            ErrorMessage = "Product not found"
                        });
                    }
                }

                _logger.LogDebug("Bulk update validation completed. {ErrorCount} errors found", errors.Count);

                return errors;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate bulk update");
                throw new InvalidOperationException($"Failed to validate bulk update: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if product should trigger low stock alert
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if alert should be triggered, false otherwise</returns>
        public async Task<bool> ShouldTriggerLowStockAlertAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                _logger.LogDebug("Checking if low stock alert should be triggered for product {ProductId}", productId);

                // Get product details
                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for low stock alert check", productId);
                    return false;
                }

                if (product.IsDeleted)
                {
                    _logger.LogDebug("Product {ProductId} is deleted, skipping low stock alert", productId);
                    return false;
                }

                // Get the appropriate threshold for this product
                var threshold = await GetLowStockThresholdAsync(product.StoreId, productId, cancellationToken);

                // Check if current stock is at or below threshold
                var shouldTrigger = product.StockQuantity <= threshold;

                if (shouldTrigger)
                {
                    _logger.LogWarning("Low stock alert triggered for product {ProductId}. Current stock: {CurrentStock}, Threshold: {Threshold}",
                        productId, product.StockQuantity, threshold);
                }
                else
                {
                    _logger.LogDebug("Low stock alert not triggered for product {ProductId}. Current stock: {CurrentStock}, Threshold: {Threshold}",
                        productId, product.StockQuantity, threshold);
                }

                return shouldTrigger;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking low stock alert for product {ProductId}", productId);
                return false; // Fail safe - don't trigger alerts on errors
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Checks if product is available for the requested quantity
        /// </summary>
        private bool IsProductAvailableForQuantity(Domain.Entities.Catalog.Product? product, int requiredQuantity)
        {
            return product != null && !product.IsDeleted && product.StockQuantity >= requiredQuantity;
        }

        /// <summary>
        /// Gets appropriate validation message based on product state
        /// </summary>
        private string GetValidationMessage(Domain.Entities.Catalog.Product? product, int requestedQuantity)
        {
            if (product == null)
            {
                return "Product not found";
            }

            if (product.IsDeleted)
            {
                return "Product is no longer available";
            }

            if (product.StockQuantity >= requestedQuantity)
            {
                return "Stock available";
            }

            return $"Only {product.StockQuantity} units available";
        }

        private async Task<int> GetLowStockThresholdAsync(Guid storeId, Guid productId, CancellationToken cancellationToken)
        {
            try
            {
                // Priority order for threshold determination:
                // 1. Product-specific threshold (highest priority)
                // 2. Store-specific threshold (medium priority)
                // 3. Category-specific threshold (if applicable)
                // 4. System-wide default (lowest priority)

                // 1. Check for product-specific threshold (cached for performance)
                if (_productThresholds.TryGetValue(productId, out var productThreshold))
                {
                    _logger.LogDebug("Using cached product-specific threshold {Threshold} for product {ProductId}",
                        productThreshold, productId);
                    return productThreshold;
                }

                // 2. Check for store-specific threshold (cached for performance)
                if (_storeThresholds.TryGetValue(storeId, out var storeThreshold))
                {
                    _logger.LogDebug("Using cached store-specific threshold {Threshold} for store {StoreId}",
                        storeThreshold, storeId);
                    return storeThreshold;
                }

                // 3. Load store-specific threshold from database
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                if (store != null)
                {
                    // In a real implementation, this would check store.LowStockThreshold
                    // For now, we'll use a calculated threshold based on store characteristics
                    var calculatedStoreThreshold = CalculateStoreThreshold(store);
                    _storeThresholds[storeId] = calculatedStoreThreshold;

                    _logger.LogDebug("Calculated store-specific threshold {Threshold} for store {StoreId}",
                        calculatedStoreThreshold, storeId);
                    return calculatedStoreThreshold;
                }

                // 4. Fall back to system-wide configuration
                var systemThreshold = GetSystemDefaultThreshold();

                _logger.LogDebug("Using system default threshold {Threshold} for product {ProductId}",
                    systemThreshold, productId);
                return systemThreshold;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock threshold for product {ProductId}, store {StoreId}",
                    productId, storeId);

                // Return a safe default on error
                return GetSystemDefaultThreshold();
            }
        }

        private int CalculateStoreThreshold(Domain.Entities.Store.Store store)
        {
            // Calculate threshold based on store characteristics
            // This is a business logic implementation that could be enhanced

            var baseThreshold = DEFAULT_LOW_STOCK_THRESHOLD;

            // Adjust based on store size/type (if we had that information)
            // For now, we'll use a simple calculation

            // In a real implementation, this might consider:
            // - Store size/capacity
            // - Store type (retail vs wholesale)
            // - Historical sales patterns
            // - Store location (urban vs rural)
            // - Product categories sold

            return baseThreshold;
        }

        private int GetSystemDefaultThreshold()
        {
            try
            {
                // Get threshold from configuration
                var configValue = _configuration[CONFIG_KEY_DEFAULT_THRESHOLD];
                var configThreshold = int.TryParse(configValue, out var parsedValue) ? parsedValue : 0;

                if (configThreshold > 0)
                {
                    _logger.LogDebug("Using configured system threshold: {Threshold}", configThreshold);
                    return configThreshold;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read low stock threshold from configuration, using hardcoded default");
            }

            // Hardcoded fallback
            _logger.LogDebug("Using hardcoded default threshold: {Threshold}", DEFAULT_LOW_STOCK_THRESHOLD);
            return DEFAULT_LOW_STOCK_THRESHOLD;
        }

        #endregion

        #region Internal Cache Management Methods

        /// <summary>
        /// Sets a product-specific low stock threshold
        /// Internal method for cache management
        /// </summary>
        internal void SetProductThreshold(Guid productId, int threshold)
        {
            if (threshold < 0)
            {
                throw new ArgumentException("Threshold cannot be negative", nameof(threshold));
            }

            _productThresholds[productId] = threshold;
            _logger.LogInformation("Set product-specific threshold {Threshold} for product {ProductId}",
                threshold, productId);
        }

        /// <summary>
        /// Sets a store-specific low stock threshold
        /// Internal method for cache management
        /// </summary>
        internal void SetStoreThreshold(Guid storeId, int threshold)
        {
            if (threshold < 0)
            {
                throw new ArgumentException("Threshold cannot be negative", nameof(threshold));
            }

            _storeThresholds[storeId] = threshold;
            _logger.LogInformation("Set store-specific threshold {Threshold} for store {StoreId}",
                threshold, storeId);
        }

        /// <summary>
        /// Clears cached thresholds (useful for testing or configuration changes)
        /// Internal method for cache management
        /// </summary>
        internal void ClearThresholdCache()
        {
            _productThresholds.Clear();
            _storeThresholds.Clear();
            _logger.LogInformation("Cleared all cached thresholds");
        }

        #endregion
    }
}
