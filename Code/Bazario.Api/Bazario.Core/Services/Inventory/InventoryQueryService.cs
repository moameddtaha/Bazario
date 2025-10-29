using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Inventory;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory read operations
    /// Handles querying inventory status, history, and reservations
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class InventoryQueryService : IInventoryQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryQueryService> _logger;

        // Constants
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;

        public InventoryQueryService(
            IUnitOfWork unitOfWork,
            ILogger<InventoryQueryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InventoryStatus> GetInventoryStatusAsync(
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

                _logger.LogDebug("Getting inventory status for product {ProductId}", productId);

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found", productId);
                    throw new KeyNotFoundException($"Product {productId} not found");
                }

                // Get reserved stock and threshold
                var reservedStock = await GetReservedStockAsync(productId, cancellationToken);
                var threshold = await GetLowStockThresholdAsync(product.StoreId, productId, cancellationToken);

                return new InventoryStatus
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    CurrentStock = product.StockQuantity,
                    ReservedStock = reservedStock,
                    LowStockThreshold = threshold,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (KeyNotFoundException)
            {
                throw; // Re-throw not found exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get inventory status for product {ProductId}", productId);
                throw new InvalidOperationException($"Failed to get inventory status: {ex.Message}", ex);
            }
        }

        public async Task<List<LowStockAlert>> GetLowStockAlertsAsync(
            Guid? storeId = null,
            int threshold = DEFAULT_LOW_STOCK_THRESHOLD,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (threshold < 0)
                {
                    throw new ArgumentException("Threshold cannot be negative", nameof(threshold));
                }

                if (storeId.HasValue && storeId.Value == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                _logger.LogDebug("Getting low stock alerts for store {StoreId} with threshold {Threshold}",
                    storeId, threshold);

                List<Domain.Entities.Catalog.Product> lowStockProducts;

                // Use optimized repository method for low stock products
                if (storeId.HasValue)
                {
                    // Get all low stock products, then filter by store
                    var allLowStockProducts = await _unitOfWork.Products.GetLowStockProductsAsync(threshold, cancellationToken);
                    lowStockProducts = allLowStockProducts.Where(p => p.StoreId == storeId.Value && !p.IsDeleted).ToList();
                }
                else
                {
                    // Get all low stock products across all stores
                    lowStockProducts = await _unitOfWork.Products.GetLowStockProductsAsync(threshold, cancellationToken);
                }

                _logger.LogInformation("Found {Count} products with low stock", lowStockProducts.Count);

                return lowStockProducts.Select(p => new LowStockAlert
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    CurrentStock = p.StockQuantity,
                    Threshold = threshold,
                    StoreId = p.StoreId,
                    IsOutOfStock = p.StockQuantity == 0,
                    AlertDate = DateTime.UtcNow
                }).ToList();
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get low stock alerts");
                throw new InvalidOperationException($"Failed to get low stock alerts: {ex.Message}", ex);
            }
        }

        public async Task<List<InventoryMovement>> GetInventoryHistoryAsync(
            Guid productId,
            DateRange? dateRange = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                _logger.LogDebug("Getting inventory history for product {ProductId}", productId);

                // Implement inventory movement tracking
                // Note: This would require a separate InventoryMovement table to track all stock changes
                // For now, return empty list as the table doesn't exist yet
                _logger.LogDebug("Inventory movement tracking not yet implemented - requires InventoryMovement table");
                return await Task.FromResult(new List<InventoryMovement>());
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get inventory history for product {ProductId}", productId);
                throw new InvalidOperationException($"Failed to get inventory history: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetActiveReservationsAsync(
            Guid? productId = null,
            Guid? storeId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId.HasValue && productId.Value == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (storeId.HasValue && storeId.Value == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                _logger.LogDebug("Getting active reservations for product {ProductId}, store {StoreId}",
                    productId, storeId);

                // Implement reservation tracking
                // Note: This would require a separate StockReservation table to track reservations
                // For now, return empty list as the table doesn't exist yet
                _logger.LogDebug("Stock reservation tracking not yet implemented - requires StockReservation table");
                return await Task.FromResult(new List<StockReservation>());
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active reservations");
                throw new InvalidOperationException($"Failed to get active reservations: {ex.Message}", ex);
            }
        }

        public async Task<StockReservation?> GetReservationByIdAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (reservationId == Guid.Empty)
                {
                    throw new ArgumentException("Reservation ID cannot be empty", nameof(reservationId));
                }

                _logger.LogDebug("Getting reservation {ReservationId}", reservationId);

                // Implement reservation tracking
                // Note: This would require a separate StockReservation table to track reservations
                // For now, return null as the table doesn't exist yet
                _logger.LogDebug("Stock reservation tracking not yet implemented - requires StockReservation table");
                return await Task.FromResult<StockReservation?>(null);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reservation {ReservationId}", reservationId);
                throw new InvalidOperationException($"Failed to get reservation: {ex.Message}", ex);
            }
        }

        public async Task<List<InventoryStatus>> GetInventoryStatusBulkAsync(
            List<Guid> productIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productIds == null)
                {
                    throw new ArgumentNullException(nameof(productIds), "Product IDs list cannot be null");
                }

                if (productIds.Count == 0)
                {
                    throw new ArgumentException("Product IDs list cannot be empty", nameof(productIds));
                }

                // Validate all product IDs
                for (int i = 0; i < productIds.Count; i++)
                {
                    if (productIds[i] == Guid.Empty)
                    {
                        throw new ArgumentException($"Product ID cannot be empty at index {i}", nameof(productIds));
                    }
                }

                _logger.LogDebug("Getting inventory status for {Count} products", productIds.Count);

                var statusList = new List<InventoryStatus>();

                // Bulk retrieve all products using GetFilteredProductsAsync with Contains predicate
                // This generates a single SQL query: WHERE ProductId IN (id1, id2, id3, ...)
                var uniqueIds = productIds.Distinct().ToList();
                var products = await _unitOfWork.Products.GetFilteredProductsAsync(
                    p => uniqueIds.Contains(p.ProductId),
                    cancellationToken);

                // Convert to dictionary for O(1) lookup
                var productDict = products.ToDictionary(p => p.ProductId);

                // Bulk retrieve thresholds for all unique stores to avoid N+1 queries
                var uniqueStoreIds = products.Select(p => p.StoreId).Distinct().ToList();
                var thresholdDict = new Dictionary<Guid, int>();
                foreach (var storeId in uniqueStoreIds)
                {
                    // Get threshold for each unique store (much fewer queries than per-product)
                    var threshold = await GetLowStockThresholdAsync(storeId, Guid.Empty, cancellationToken);
                    thresholdDict[storeId] = threshold;
                }

                // Reserved stock is 0 for all products (stub implementation)
                var reservedStock = 0;

                // Process each requested product
                int failedCount = 0;
                foreach (var productId in productIds)
                {
                    if (productDict.TryGetValue(productId, out var product))
                    {
                        try
                        {
                            // Use cached threshold value (O(1) lookup)
                            var threshold = thresholdDict.GetValueOrDefault(product.StoreId, DEFAULT_LOW_STOCK_THRESHOLD);

                            statusList.Add(new InventoryStatus
                            {
                                ProductId = productId,
                                ProductName = product.Name,
                                CurrentStock = product.StockQuantity,
                                ReservedStock = reservedStock,
                                LowStockThreshold = threshold,
                                LastUpdated = DateTime.UtcNow
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing inventory status for product {ProductId}", productId);
                            failedCount++;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Product {ProductId} not found in bulk query", productId);
                        failedCount++;
                    }
                }

                if (failedCount > 0)
                {
                    _logger.LogWarning("Failed to retrieve inventory status for {FailedCount} of {TotalCount} products",
                        failedCount, productIds.Count);
                }

                _logger.LogDebug("Retrieved inventory status for {SuccessCount} of {TotalCount} products",
                    statusList.Count, productIds.Count);

                return statusList;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bulk inventory status");
                throw new InvalidOperationException($"Failed to get bulk inventory status: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the total reserved stock quantity for a product
        /// Note: Returns 0 until StockReservation table is implemented
        /// </summary>
        private Task<int> GetReservedStockAsync(Guid productId, CancellationToken cancellationToken)
        {
            // In a real implementation, this would query the StockReservation table
            // to get the total reserved quantity for this product
            // For now, return 0 as reservation tracking is not implemented
            return Task.FromResult(0);
        }

        /// <summary>
        /// Gets the low stock threshold for a product in a store
        /// Priority order: Product-specific → Store-specific → System default
        /// Note: Returns system default until threshold configuration is implemented
        /// </summary>
        private Task<int> GetLowStockThresholdAsync(Guid storeId, Guid productId, CancellationToken cancellationToken)
        {
            // In a real implementation, this would:
            // 1. Check if the product has a specific low stock threshold
            // 2. Check if the store has a default low stock threshold
            // 3. Fall back to a system-wide default

            // For now, return a default threshold
            return Task.FromResult(DEFAULT_LOW_STOCK_THRESHOLD);
        }
    }
}
