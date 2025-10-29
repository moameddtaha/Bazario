using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory CRUD operations
    /// Handles stock updates, reservations, and basic inventory management
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class InventoryManagementService : IInventoryManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryManagementService> _logger;

        // Constants for default values and limits
        private const int DEFAULT_RESERVATION_EXPIRY_MINUTES = 30;
        private const int DEFAULT_CLEANUP_EXPIRY_MINUTES = 30;
        private const int MIN_STOCK_QUANTITY = 0;
        private const int MAX_STOCK_QUANTITY = 1000000;
        private const int MIN_THRESHOLD_VALUE = 0;
        private const int MAX_THRESHOLD_VALUE = 10000;

        public InventoryManagementService(
            IUnitOfWork unitOfWork,
            ILogger<InventoryManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InventoryUpdateResult> UpdateStockAsync(
            Guid productId,
            int newQuantity,
            StockUpdateType updateType,
            string reason,
            Guid updatedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update stock with empty product ID");
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (newQuantity < MIN_STOCK_QUANTITY || newQuantity > MAX_STOCK_QUANTITY)
                {
                    _logger.LogWarning("Invalid stock quantity: {NewQuantity}. Must be between {Min} and {Max}",
                        newQuantity, MIN_STOCK_QUANTITY, MAX_STOCK_QUANTITY);
                    throw new ArgumentException($"Stock quantity must be between {MIN_STOCK_QUANTITY} and {MAX_STOCK_QUANTITY}", nameof(newQuantity));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    _logger.LogWarning("Attempted to update stock without providing a reason");
                    throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
                }

                if (updatedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update stock without valid updater ID");
                    throw new ArgumentException("UpdatedBy cannot be empty", nameof(updatedBy));
                }

                _logger.LogDebug("Updating stock for product {ProductId} to {NewQuantity}. Type: {UpdateType}, Reason: {Reason}",
                    productId, newQuantity, updateType, reason);

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for stock update", productId);
                    return new InventoryUpdateResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Product not found"
                    };
                }

                var previousQuantity = product.StockQuantity;

                // Update stock based on type
                product.StockQuantity = updateType switch
                {
                    StockUpdateType.Purchase => product.StockQuantity + newQuantity,
                    StockUpdateType.Sale => Math.Max(MIN_STOCK_QUANTITY, product.StockQuantity - newQuantity),
                    StockUpdateType.Adjustment => newQuantity,
                    StockUpdateType.Return => product.StockQuantity + newQuantity,
                    StockUpdateType.Damage => Math.Max(MIN_STOCK_QUANTITY, product.StockQuantity - newQuantity),
                    _ => product.StockQuantity
                };

                await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated stock for product {ProductId} from {PreviousQuantity} to {NewQuantity}",
                    productId, previousQuantity, product.StockQuantity);

                return new InventoryUpdateResult
                {
                    IsSuccessful = true,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = product.StockQuantity,
                    MovementId = Guid.NewGuid()
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating stock for product: {ProductId}", productId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating stock for product: {ProductId}", productId);
                throw new InvalidOperationException($"Unexpected error while updating stock for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<StockReservationResult> ReserveStockAsync(
            StockReservationRequest reservationRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (reservationRequest == null)
                {
                    _logger.LogWarning("Attempted to reserve stock with null reservation request");
                    throw new ArgumentNullException(nameof(reservationRequest), "Reservation request cannot be null");
                }

                if (reservationRequest.Items == null || reservationRequest.Items.Count == 0)
                {
                    _logger.LogWarning("Attempted to reserve stock with null or empty items list");
                    throw new ArgumentException("Reservation request must contain at least one item", nameof(reservationRequest));
                }

                if (reservationRequest.CustomerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to reserve stock without valid customer ID");
                    throw new ArgumentException("Customer ID cannot be empty", nameof(reservationRequest));
                }

                // Validate each item
                for (int i = 0; i < reservationRequest.Items.Count; i++)
                {
                    var item = reservationRequest.Items[i];

                    if (item.ProductId == Guid.Empty)
                    {
                        throw new ArgumentException($"Product ID cannot be empty at index {i}", nameof(reservationRequest));
                    }

                    if (item.Quantity <= 0)
                    {
                        throw new ArgumentException($"Quantity must be greater than 0 at index {i}", nameof(reservationRequest));
                    }
                }

                _logger.LogDebug("Reserving stock for {ItemCount} items, Customer: {CustomerId}",
                    reservationRequest.Items.Count, reservationRequest.CustomerId);

                var result = new StockReservationResult
                {
                    ReservationId = Guid.NewGuid(),
                    IsSuccessful = true,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(DEFAULT_RESERVATION_EXPIRY_MINUTES)
                };

                foreach (var item in reservationRequest.Items)
                {
                    var product = await _unitOfWork.Products.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        result.IsSuccessful = false;
                        result.ItemResults.Add(new ReservationStatus
                        {
                            ProductId = item.ProductId,
                            RequestedQuantity = item.Quantity,
                            ReservedQuantity = 0,
                            IsFullyReserved = false,
                            ErrorMessage = product == null ? "Product not found" : "Insufficient stock"
                        });
                        continue;
                    }

                    result.ItemResults.Add(new ReservationStatus
                    {
                        ProductId = item.ProductId,
                        RequestedQuantity = item.Quantity,
                        ReservedQuantity = item.Quantity,
                        IsFullyReserved = true
                    });
                }

                if (!result.IsSuccessful)
                {
                    _logger.LogWarning("Stock reservation partially failed. {FailedCount} items could not be reserved",
                        result.ItemResults.Count(r => !r.IsFullyReserved));
                }
                else
                {
                    _logger.LogInformation("Successfully reserved stock for {ItemCount} items. ReservationId: {ReservationId}",
                        reservationRequest.Items.Count, result.ReservationId);
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while reserving stock");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while reserving stock for customer: {CustomerId}", reservationRequest?.CustomerId);
                throw new InvalidOperationException($"Unexpected error while reserving stock: {ex.Message}", ex);
            }
        }

        public async Task<bool> ReleaseReservationAsync(
            Guid reservationId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to release reservation with empty reservation ID");
                    throw new ArgumentException("Reservation ID cannot be empty", nameof(reservationId));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    _logger.LogWarning("Attempted to release reservation without providing a reason");
                    throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
                }

                _logger.LogDebug("Releasing reservation {ReservationId}. Reason: {Reason}", reservationId, reason);

                // Implement reservation tracking and release logic
                // Note: This would require a StockReservation table to track reservations
                // For now, log the action and return true as placeholder
                _logger.LogInformation("Reservation release not yet fully implemented - requires StockReservation table");
                await Task.CompletedTask;
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while releasing reservation: {ReservationId}", reservationId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while releasing reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while releasing reservation {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ConfirmReservationAsync(
            Guid reservationId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to confirm reservation with empty reservation ID");
                    throw new ArgumentException("Reservation ID cannot be empty", nameof(reservationId));
                }

                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to confirm reservation with empty order ID");
                    throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
                }

                _logger.LogDebug("Confirming reservation {ReservationId} for order {OrderId}", reservationId, orderId);

                // Implement reservation confirmation logic
                // This should convert reservation to actual sale and update stock
                // Note: This would require a StockReservation table to track reservations
                _logger.LogInformation("Reservation confirmation not yet fully implemented - requires StockReservation table");
                await Task.CompletedTask;
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while confirming reservation: {ReservationId}", reservationId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while confirming reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while confirming reservation {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<BulkInventoryUpdateResult> BulkUpdateStockAsync(
            BulkStockUpdateRequest bulkUpdateRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (bulkUpdateRequest == null)
                {
                    _logger.LogWarning("Attempted to perform bulk update with null request");
                    throw new ArgumentNullException(nameof(bulkUpdateRequest), "Bulk update request cannot be null");
                }

                if (bulkUpdateRequest.Items == null || bulkUpdateRequest.Items.Count == 0)
                {
                    _logger.LogWarning("Attempted to perform bulk update with null or empty items list");
                    throw new ArgumentException("Bulk update request must contain at least one item", nameof(bulkUpdateRequest));
                }

                if (bulkUpdateRequest.UpdatedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to perform bulk update without valid updater ID");
                    throw new ArgumentException("UpdatedBy cannot be empty", nameof(bulkUpdateRequest));
                }

                _logger.LogDebug("Processing bulk stock update for {ItemCount} items", bulkUpdateRequest.Items.Count);

                var result = new BulkInventoryUpdateResult
                {
                    TotalItems = bulkUpdateRequest.Items.Count
                };

                foreach (var item in bulkUpdateRequest.Items)
                {
                    try
                    {
                        var updateResult = await UpdateStockAsync(
                            item.ProductId,
                            item.NewQuantity,
                            StockUpdateType.Adjustment,
                            bulkUpdateRequest.Reason ?? "Bulk update",
                            bulkUpdateRequest.UpdatedBy,
                            cancellationToken);

                        if (updateResult.IsSuccessful)
                        {
                            result.SuccessfulUpdates++;
                        }
                        else
                        {
                            result.FailedUpdates++;
                            result.Errors.Add(new BulkUpdateError
                            {
                                ProductId = item.ProductId,
                                ErrorMessage = updateResult.ErrorMessage ?? "Unknown error"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating stock for product {ProductId} in bulk operation", item.ProductId);
                        result.FailedUpdates++;
                        result.Errors.Add(new BulkUpdateError
                        {
                            ProductId = item.ProductId,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                _logger.LogInformation("Bulk stock update completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    result.SuccessfulUpdates, result.FailedUpdates);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while performing bulk stock update");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while performing bulk stock update");
                throw new InvalidOperationException($"Unexpected error while performing bulk stock update: {ex.Message}", ex);
            }
        }

        public async Task<bool> SetLowStockThresholdAsync(
            Guid productId,
            int threshold,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to set low stock threshold with empty product ID");
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (threshold < MIN_THRESHOLD_VALUE || threshold > MAX_THRESHOLD_VALUE)
                {
                    _logger.LogWarning("Invalid threshold value: {Threshold}. Must be between {Min} and {Max}",
                        threshold, MIN_THRESHOLD_VALUE, MAX_THRESHOLD_VALUE);
                    throw new ArgumentException($"Threshold must be between {MIN_THRESHOLD_VALUE} and {MAX_THRESHOLD_VALUE}", nameof(threshold));
                }

                _logger.LogDebug("Setting low stock threshold for product {ProductId} to {Threshold}", productId, threshold);

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for threshold update", productId);
                    return false;
                }

                // Add threshold field to Product entity or create separate threshold tracking
                // For now, use a default threshold and return true as placeholder
                // In a real implementation, this would check:
                // - Product.LowStockThreshold (if added to Product entity)
                // - Store.LowStockThreshold (if added to Store entity)
                // - System configuration settings
                _logger.LogInformation("Low stock threshold tracking not yet fully implemented - requires threshold fields");
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while setting low stock threshold for product: {ProductId}", productId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while setting low stock threshold for product: {ProductId}", productId);
                throw new InvalidOperationException($"Unexpected error while setting low stock threshold for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> CleanupExpiredReservationsAsync(
            int expirationMinutes = DEFAULT_CLEANUP_EXPIRY_MINUTES,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Input validation
                if (expirationMinutes <= 0)
                {
                    _logger.LogWarning("Attempted to cleanup expired reservations with invalid expiration minutes: {Minutes}", expirationMinutes);
                    throw new ArgumentException("Expiration minutes must be greater than 0", nameof(expirationMinutes));
                }

                _logger.LogDebug("Cleaning up expired reservations older than {Minutes} minutes", expirationMinutes);

                // Implement expired reservation cleanup logic
                // This should release stock from reservations that have expired
                // Note: This would require a StockReservation table to track reservations
                _logger.LogInformation("Expired reservation cleanup not yet fully implemented - requires StockReservation table");
                await Task.CompletedTask;
                return 0;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while cleaning up expired reservations");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while cleaning up expired reservations");
                throw new InvalidOperationException($"Unexpected error while cleaning up expired reservations: {ex.Message}", ex);
            }
        }
    }
}
