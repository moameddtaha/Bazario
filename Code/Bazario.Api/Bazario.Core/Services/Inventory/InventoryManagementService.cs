using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory management operations for e-commerce stock control
    /// Provides 7 core methods: UpdateStockAsync, ReserveStockAsync, ReleaseReservationAsync,
    /// ConfirmReservationAsync, BulkUpdateStockAsync, SetLowStockThresholdAsync, CleanupExpiredReservationsAsync
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// All operations are transactional with automatic rollback on errors
    /// </summary>
    public class InventoryManagementService : IInventoryManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly ILogger<InventoryManagementService> _logger;

        // Constants for default values and limits
        private const int DEFAULT_RESERVATION_EXPIRY_MINUTES = 30;
        private const int DEFAULT_CLEANUP_EXPIRY_MINUTES = 30;
        private const int MIN_STOCK_QUANTITY = 0;
        private const int MAX_STOCK_QUANTITY = 1000000;
        private const int MIN_THRESHOLD_VALUE = 0;
        private const int MAX_THRESHOLD_VALUE = 10000;
        private const int MAX_RESERVATION_ITEMS = 100;
        private const int MAX_BULK_UPDATE_ITEMS = 1000;

        // Constants for reservation status values (replaces magic strings)
        private const string STATUS_PENDING = "Pending";
        private const string STATUS_CONFIRMED = "Confirmed";
        private const string STATUS_RELEASED = "Released";
        private const string STATUS_EXPIRED = "Expired";

        public InventoryManagementService(
            IUnitOfWork unitOfWork,
            IConcurrencyHelper concurrencyHelper,
            ILogger<InventoryManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
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
            // Input validation (outside retry)
            if (productId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Product ID cannot be empty");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            if (newQuantity < MIN_STOCK_QUANTITY || newQuantity > MAX_STOCK_QUANTITY)
            {
                _logger.LogDebug("Validation failed: Invalid stock quantity {NewQuantity}. Must be between {Min} and {Max}",
                    newQuantity, MIN_STOCK_QUANTITY, MAX_STOCK_QUANTITY);
                throw new ArgumentException($"Stock quantity must be between {MIN_STOCK_QUANTITY} and {MAX_STOCK_QUANTITY}", nameof(newQuantity));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogDebug("Validation failed: Reason cannot be null or empty");
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
            }

            if (updatedBy == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: UpdatedBy cannot be empty");
                throw new ArgumentException("UpdatedBy cannot be empty", nameof(updatedBy));
            }

            _logger.LogDebug("Updating stock for product {ProductId} to {NewQuantity}. Type: {UpdateType}, Reason: {Reason}",
                productId, newQuantity, updateType, reason);

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Get product (gets fresh RowVersion on each retry)
                        var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                        if (product == null || product.IsDeleted)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogWarning("Product {ProductId} not found or deleted for stock update", productId);
                            return new InventoryUpdateResult
                            {
                                IsSuccessful = false,
                                ErrorMessage = product == null ? "Product not found" : "Product has been deleted"
                            };
                        }

                        var previousQuantity = product.StockQuantity;

                        // Update stock based on type with overflow validation
                        int calculatedStock = updateType switch
                        {
                            StockUpdateType.Purchase => product.StockQuantity + newQuantity,
                            StockUpdateType.Sale => Math.Max(MIN_STOCK_QUANTITY, product.StockQuantity - newQuantity),
                            StockUpdateType.Adjustment => newQuantity,
                            StockUpdateType.Return => product.StockQuantity + newQuantity,
                            StockUpdateType.Damage => Math.Max(MIN_STOCK_QUANTITY, product.StockQuantity - newQuantity),
                            StockUpdateType.Transfer => Math.Max(MIN_STOCK_QUANTITY, product.StockQuantity - newQuantity),
                            StockUpdateType.Correction => newQuantity,
                            _ => throw new ArgumentException($"Unsupported stock update type: {updateType}", nameof(updateType))
                        };

                        // Validate that calculated stock doesn't exceed maximum allowed
                        if (calculatedStock > MAX_STOCK_QUANTITY)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogWarning("Stock update would exceed maximum: {CalculatedStock} > {MaxStock}", calculatedStock, MAX_STOCK_QUANTITY);
                            throw new ArgumentException($"Resulting stock quantity {calculatedStock} exceeds maximum allowed {MAX_STOCK_QUANTITY}");
                        }

                        product.StockQuantity = calculatedStock;

                        await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "UpdateStock", cancellationToken);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while updating stock for product: {ProductId}", productId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating stock for product: {ProductId}", productId);
                throw new InvalidOperationException("Unexpected error while updating stock", ex);
            }
        }

        public async Task<StockReservationResult> ReserveStockAsync(
            StockReservationRequest reservationRequest,
            CancellationToken cancellationToken = default)
        {
            // Input validation (outside retry)
            if (reservationRequest == null)
            {
                _logger.LogDebug("Validation failed: Reservation request cannot be null");
                throw new ArgumentNullException(nameof(reservationRequest), "Reservation request cannot be null");
            }

            if (reservationRequest.Items == null || reservationRequest.Items.Count == 0)
            {
                _logger.LogDebug("Validation failed: Reservation request must contain at least one item");
                throw new ArgumentException("Reservation request must contain at least one item", nameof(reservationRequest));
            }

            if (reservationRequest.Items.Count > MAX_RESERVATION_ITEMS)
            {
                _logger.LogDebug("Validation failed: Attempted to reserve {ItemCount} items, exceeding maximum of {MaxItems}",
                    reservationRequest.Items.Count, MAX_RESERVATION_ITEMS);
                throw new ArgumentException($"Reservation request cannot exceed {MAX_RESERVATION_ITEMS} items. Received {reservationRequest.Items.Count} items.", nameof(reservationRequest));
            }

            if (reservationRequest.CustomerId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Customer ID cannot be empty");
                throw new ArgumentException("Customer ID cannot be empty", nameof(reservationRequest));
            }

            // Validate each item (outside retry)
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

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {

                // Bulk retrieve all products to avoid N+1 query problem
                // Inside transaction to ensure consistent read with database locking
                var productIds = reservationRequest.Items.Select(i => i.ProductId).ToList();
                var productsDict = await GetProductsByIdsAsync(productIds, cancellationToken);

                // Use the expiration minutes from the request (defaults to 30 if not specified)
                var expirationMinutes = reservationRequest.ExpirationMinutes > 0
                    ? reservationRequest.ExpirationMinutes
                    : DEFAULT_RESERVATION_EXPIRY_MINUTES;

                        var stockReservationResult = new StockReservationResult
                        {
                            ReservationId = Guid.NewGuid(),
                            IsSuccessful = true,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                        };

                        var reservationsToCreate = new List<Domain.Entities.Inventory.StockReservation>();

                        // First pass: Validate all items and check stock availability
                        foreach (var item in reservationRequest.Items)
                {
                    // O(1) dictionary lookup instead of database query
                    productsDict.TryGetValue(item.ProductId, out var product);

                            if (product == null || product.IsDeleted || product.StockQuantity < item.Quantity)
                            {
                                stockReservationResult.IsSuccessful = false;

                                string errorMessage = product == null
                                    ? "Product not found"
                                    : product.IsDeleted
                                        ? "Product has been deleted"
                                        : "Insufficient stock";

                                stockReservationResult.ItemResults.Add(new ReservationStatus
                                {
                                    ProductId = item.ProductId,
                                    RequestedQuantity = item.Quantity,
                                    ReservedQuantity = 0,
                                    IsFullyReserved = false,
                                    ErrorMessage = errorMessage
                                });
                            }
                            else
                            {
                                stockReservationResult.ItemResults.Add(new ReservationStatus
                                {
                                    ProductId = item.ProductId,
                                    RequestedQuantity = item.Quantity,
                                    ReservedQuantity = item.Quantity,
                                    IsFullyReserved = true
                                });
                            }
                        }

                        // If any item failed validation, rollback and return
                        if (!stockReservationResult.IsSuccessful)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogWarning("Stock reservation failed validation. {FailedCount} items could not be reserved",
                                stockReservationResult.ItemResults.Count(r => !r.IsFullyReserved));
                            return stockReservationResult;
                        }

                        // Second pass: Create reservation entities and update product stock
                        var reservationId = stockReservationResult.ReservationId.Value;
                        var now = DateTime.UtcNow;
                        var expiresAt = stockReservationResult.ExpiresAt.Value;

                        foreach (var item in reservationRequest.Items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Defensive programming: Use TryGetValue even though we validated earlier
                            if (!productsDict.TryGetValue(item.ProductId, out var product))
                            {
                                _logger.LogError("Product {ProductId} unexpectedly missing from dictionary during reservation creation", item.ProductId);
                                throw new InvalidOperationException($"Product {item.ProductId} missing during reservation - data integrity issue");
                            }

                            // Create StockReservation entity for database persistence
                            // Note: We create one record per product in the reservation
                            var reservation = new Domain.Entities.Inventory.StockReservation
                            {
                                Id = Guid.NewGuid(), // Unique primary key for this record
                                ReservationId = reservationId, // Same ID for all items in this reservation request
                                ProductId = item.ProductId,
                                CustomerId = reservationRequest.CustomerId,
                                ReservedQuantity = item.Quantity,
                                Status = STATUS_PENDING,
                                CreatedAt = now,
                                ExpiresAt = expiresAt,
                                ExternalReference = reservationRequest.OrderReference ?? string.Empty,
                                OrderId = null, // Not linked to order yet
                                IsDeleted = false
                            };

                            reservationsToCreate.Add(reservation);

                            // Deduct stock quantity (reserve the stock)
                            // Use Math.Max to ensure stock never goes negative (defensive programming)
                            product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                            await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                            _logger.LogDebug("Reserved {Quantity} units of product {ProductId}. New stock: {NewStock}",
                                item.Quantity, item.ProductId, product.StockQuantity);
                        }

                        // Save all reservations to database
                        foreach (var reservation in reservationsToCreate)
                        {
                            await _unitOfWork.StockReservations.AddReservationAsync(reservation, cancellationToken);
                        }

                        // Commit transaction - all changes are permanent
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        return stockReservationResult;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "ReserveStock", cancellationToken);

                _logger.LogInformation("Successfully reserved stock for {ItemCount} items. ReservationId: {ReservationId}, CustomerId: {CustomerId}",
                    reservationRequest.Items.Count, result.ReservationId, reservationRequest.CustomerId);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while reserving stock");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while reserving stock for customer: {CustomerId}", reservationRequest?.CustomerId);
                throw new InvalidOperationException("Unexpected error while reserving stock", ex);
            }
        }

        public async Task<bool> ReleaseReservationAsync(
            Guid reservationId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            // Input validation (outside retry)
            if (reservationId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Reservation ID cannot be empty");
                throw new ArgumentException("Reservation ID cannot be empty", nameof(reservationId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogDebug("Validation failed: Reason cannot be null or empty");
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
            }

            _logger.LogDebug("Releasing reservation {ReservationId}. Reason: {Reason}", reservationId, reason);

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Retrieve all reservation records for this reservation ID (gets fresh RowVersion on each retry)
                        var reservations = await _unitOfWork.StockReservations.GetFilteredReservationsAsync(
                            r => r.ReservationId == reservationId && r.Status == STATUS_PENDING && !r.IsDeleted,
                            cancellationToken);

                        if (reservations == null || reservations.Count == 0)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogWarning("No pending reservations found with ID: {ReservationId}", reservationId);
                            return false;
                        }

                        _logger.LogDebug("Found {Count} pending reservation records to release", reservations.Count);

                        // Bulk retrieve all products to avoid N+1 query
                        var productIds = reservations.Select(r => r.ProductId).ToList();
                        var productsDict = await GetProductsByIdsAsync(productIds, cancellationToken);

                        // Restore stock quantities and update reservation status
                        var now = DateTime.UtcNow;
                        foreach (var reservation in reservations)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Restore stock quantity (only if product exists and is not deleted)
                            if (productsDict.TryGetValue(reservation.ProductId, out var product))
                            {
                                if (!product.IsDeleted)
                                {
                                    product.StockQuantity += reservation.ReservedQuantity;
                                    await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                                    _logger.LogDebug("Restored {Quantity} units to product {ProductId}. New stock: {NewStock}",
                                        reservation.ReservedQuantity, reservation.ProductId, product.StockQuantity);
                                }
                                else
                                {
                                    _logger.LogWarning("Product {ProductId} is deleted, skipping stock restoration for reservation {ReservationId}",
                                        reservation.ProductId, reservationId);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Product {ProductId} not found when releasing reservation {ReservationId}",
                                    reservation.ProductId, reservationId);
                            }

                            // Update reservation status
                            reservation.Status = STATUS_RELEASED;
                            reservation.ReleasedAt = now;
                            await _unitOfWork.StockReservations.UpdateReservationAsync(reservation, cancellationToken);
                        }

                        // Commit all changes
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        return true;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "ReleaseReservation", cancellationToken);

                _logger.LogInformation("Successfully released reservation {ReservationId}. Reason: {Reason}",
                    reservationId, reason);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while releasing reservation: {ReservationId}", reservationId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while releasing reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException("Unexpected error while releasing reservation", ex);
            }
        }

        public async Task<bool> ConfirmReservationAsync(
            Guid reservationId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            // Input validation (outside retry)
            if (reservationId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Reservation ID cannot be empty");
                throw new ArgumentException("Reservation ID cannot be empty", nameof(reservationId));
            }

            if (orderId == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: Order ID cannot be empty");
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            _logger.LogDebug("Confirming reservation {ReservationId} for order {OrderId}", reservationId, orderId);

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Retrieve all pending reservation records for this reservation ID (gets fresh RowVersion on each retry)
                        var reservations = await _unitOfWork.StockReservations.GetFilteredReservationsAsync(
                            r => r.ReservationId == reservationId && r.Status == STATUS_PENDING && !r.IsDeleted,
                            cancellationToken);

                        if (reservations == null || reservations.Count == 0)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogWarning("No pending reservations found with ID: {ReservationId}", reservationId);
                            return false;
                        }

                        _logger.LogDebug("Found {Count} pending reservation records to confirm for order {OrderId}",
                            reservations.Count, orderId);

                        // Confirm all reservation records (link to order and mark as confirmed)
                        var now = DateTime.UtcNow;
                        foreach (var reservation in reservations)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Update reservation status and link to order
                            reservation.Status = STATUS_CONFIRMED;
                            reservation.ConfirmedAt = now;
                            reservation.OrderId = orderId;
                            await _unitOfWork.StockReservations.UpdateReservationAsync(reservation, cancellationToken);

                            _logger.LogDebug("Confirmed reservation for product {ProductId}, quantity {Quantity}, linked to order {OrderId}",
                                reservation.ProductId, reservation.ReservedQuantity, orderId);
                        }

                        // Commit all changes
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        return true;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "ConfirmReservation", cancellationToken);

                _logger.LogInformation("Successfully confirmed reservation {ReservationId} for order {OrderId}",
                    reservationId, orderId);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while confirming reservation: {ReservationId}", reservationId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while confirming reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException("Unexpected error while confirming reservation", ex);
            }
        }

        public async Task<BulkInventoryUpdateResult> BulkUpdateStockAsync(
            BulkStockUpdateRequest bulkUpdateRequest,
            CancellationToken cancellationToken = default)
        {
            // Input validation (outside retry)
            if (bulkUpdateRequest == null)
            {
                _logger.LogDebug("Validation failed: Bulk update request cannot be null");
                throw new ArgumentNullException(nameof(bulkUpdateRequest), "Bulk update request cannot be null");
            }

            if (bulkUpdateRequest.Items == null || bulkUpdateRequest.Items.Count == 0)
            {
                _logger.LogDebug("Validation failed: Bulk update request must contain at least one item");
                throw new ArgumentException("Bulk update request must contain at least one item", nameof(bulkUpdateRequest));
            }

            if (bulkUpdateRequest.Items.Count > MAX_BULK_UPDATE_ITEMS)
            {
                _logger.LogDebug("Validation failed: Attempted to bulk update {ItemCount} items, exceeding maximum of {MaxItems}",
                    bulkUpdateRequest.Items.Count, MAX_BULK_UPDATE_ITEMS);
                throw new ArgumentException($"Bulk update request cannot exceed {MAX_BULK_UPDATE_ITEMS} items. Received {bulkUpdateRequest.Items.Count} items.", nameof(bulkUpdateRequest));
            }

            if (bulkUpdateRequest.UpdatedBy == Guid.Empty)
            {
                _logger.LogDebug("Validation failed: UpdatedBy cannot be empty");
                throw new ArgumentException("UpdatedBy cannot be empty", nameof(bulkUpdateRequest));
            }

            _logger.LogDebug("Processing bulk stock update for {ItemCount} items", bulkUpdateRequest.Items.Count);

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Bulk retrieve all products to avoid N+1 query problem (gets fresh RowVersion on each retry)
                        var productIds = bulkUpdateRequest.Items.Select(i => i.ProductId).ToList();
                        var productsDict = await GetProductsByIdsAsync(productIds, cancellationToken);

                        var bulkResult = new BulkInventoryUpdateResult
                        {
                            TotalItems = bulkUpdateRequest.Items.Count
                        };

                        foreach (var item in bulkUpdateRequest.Items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                // O(1) dictionary lookup instead of database query
                                if (!productsDict.TryGetValue(item.ProductId, out var product))
                                {
                                    bulkResult.FailedUpdates++;
                                    bulkResult.Errors.Add(new BulkUpdateError
                                    {
                                        ProductId = item.ProductId,
                                        ErrorMessage = "Product not found"
                                    });
                                    continue;
                                }

                                // Check if product is deleted
                                if (product.IsDeleted)
                                {
                                    bulkResult.FailedUpdates++;
                                    bulkResult.Errors.Add(new BulkUpdateError
                                    {
                                        ProductId = item.ProductId,
                                        ErrorMessage = "Product has been deleted"
                                    });
                                    continue;
                                }

                                // Validate quantity range
                                if (item.NewQuantity < MIN_STOCK_QUANTITY || item.NewQuantity > MAX_STOCK_QUANTITY)
                                {
                                    bulkResult.FailedUpdates++;
                                    bulkResult.Errors.Add(new BulkUpdateError
                                    {
                                        ProductId = item.ProductId,
                                        ErrorMessage = $"Stock quantity must be between {MIN_STOCK_QUANTITY} and {MAX_STOCK_QUANTITY}"
                                    });
                                    continue;
                                }

                                // Update stock (Adjustment type - sets to exact value)
                                product.StockQuantity = item.NewQuantity;
                                await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                                bulkResult.SuccessfulUpdates++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error updating stock for product {ProductId} in bulk operation", item.ProductId);
                                bulkResult.FailedUpdates++;
                                bulkResult.Errors.Add(new BulkUpdateError
                                {
                                    ProductId = item.ProductId,
                                    ErrorMessage = ex.Message
                                });
                            }
                        }

                        // Commit transaction if any updates succeeded
                        if (bulkResult.SuccessfulUpdates > 0)
                        {
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                        }
                        else
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        }

                        return bulkResult;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "BulkUpdateStock", cancellationToken);

                _logger.LogInformation("Bulk stock update completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    result.SuccessfulUpdates, result.FailedUpdates);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while performing bulk stock update");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while performing bulk stock update");
                throw new InvalidOperationException("Unexpected error while performing bulk stock update", ex);
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
                    _logger.LogDebug("Validation failed: Product ID cannot be empty");
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (threshold < MIN_THRESHOLD_VALUE || threshold > MAX_THRESHOLD_VALUE)
                {
                    _logger.LogDebug("Validation failed: Invalid threshold {Threshold}. Must be between {Min} and {Max}",
                        threshold, MIN_THRESHOLD_VALUE, MAX_THRESHOLD_VALUE);
                    throw new ArgumentException($"Threshold must be between {MIN_THRESHOLD_VALUE} and {MAX_THRESHOLD_VALUE}", nameof(threshold));
                }

                _logger.LogDebug("Setting low stock threshold for product {ProductId} to {Threshold}", productId, threshold);

                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null || product.IsDeleted)
                {
                    _logger.LogWarning("Product {ProductId} not found or deleted for threshold update", productId);
                    return false;
                }

                // TODO: Implement low stock threshold tracking
                // This feature requires adding a LowStockThreshold field to the Product entity:
                // 1. Add "public int? LowStockThreshold { get; set; }" to Product entity
                // 2. Create and apply EF Core migration
                // 3. Update this method to: product.LowStockThreshold = threshold; await SaveChangesAsync();
                // 4. Create low stock alert service that queries products where StockQuantity <= LowStockThreshold

                _logger.LogWarning("SetLowStockThresholdAsync called but feature not implemented - requires Product entity schema changes");
                throw new NotImplementedException(
                    "Low stock threshold tracking requires Product entity schema changes. " +
                    "Add LowStockThreshold field to Product entity and create migration before using this feature.");
            }
            catch (NotImplementedException)
            {
                // Re-throw NotImplementedException as-is (don't wrap it)
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while setting low stock threshold for product: {ProductId}", productId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while setting low stock threshold for product: {ProductId}", productId);
                throw new InvalidOperationException("Unexpected error while setting low stock threshold", ex);
            }
        }

        public async Task<int> CleanupExpiredReservationsAsync(
            int expirationMinutes = DEFAULT_CLEANUP_EXPIRY_MINUTES,
            CancellationToken cancellationToken = default)
        {
            // Input validation (outside retry)
            if (expirationMinutes <= 0)
            {
                _logger.LogDebug("Validation failed: Expiration minutes {Minutes} must be greater than 0", expirationMinutes);
                throw new ArgumentException("Expiration minutes must be greater than 0", nameof(expirationMinutes));
            }

            _logger.LogDebug("Cleaning up expired reservations older than {Minutes} minutes", expirationMinutes);

            try
            {
                // Wrap entire transaction in retry for optimistic concurrency control
                var totalReservations = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Start transaction (INSIDE retry - fresh transaction each retry)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Query all expired pending reservations (gets fresh RowVersion on each retry)
                        var now = DateTime.UtcNow;
                        var expiredReservations = await _unitOfWork.StockReservations.GetFilteredReservationsAsync(
                            r => r.Status == STATUS_PENDING && r.ExpiresAt < now && !r.IsDeleted,
                            cancellationToken);

                        if (expiredReservations == null || expiredReservations.Count == 0)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogDebug("No expired reservations found to cleanup");
                            return 0;
                        }

                        _logger.LogInformation("Found {Count} expired reservations to cleanup", expiredReservations.Count);

                        // Bulk retrieve all products to avoid N+1 query
                        var productIds = expiredReservations.Select(r => r.ProductId).ToList();
                        var productsDict = await GetProductsByIdsAsync(productIds, cancellationToken);

                        // Track cleanup statistics
                        int restoredCount = 0;
                        int totalCount = expiredReservations.Count;

                        // Restore stock and mark reservations as expired
                        foreach (var reservation in expiredReservations)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Restore stock quantity (only if product exists and is not deleted)
                            if (productsDict.TryGetValue(reservation.ProductId, out var product))
                            {
                                if (!product.IsDeleted)
                                {
                                    product.StockQuantity += reservation.ReservedQuantity;
                                    await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                                    _logger.LogDebug("Restored {Quantity} units to product {ProductId} from expired reservation {ReservationId}. New stock: {NewStock}",
                                        reservation.ReservedQuantity, reservation.ProductId, reservation.ReservationId, product.StockQuantity);

                                    restoredCount++;
                                }
                                else
                                {
                                    _logger.LogWarning("Product {ProductId} is deleted, skipping stock restoration for expired reservation {ReservationId}",
                                        reservation.ProductId, reservation.ReservationId);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Product {ProductId} not found when cleaning up expired reservation {ReservationId}",
                                    reservation.ProductId, reservation.ReservationId);
                            }

                            // Update reservation status to Expired
                            reservation.Status = STATUS_EXPIRED;
                            reservation.ReleasedAt = now;
                            await _unitOfWork.StockReservations.UpdateReservationAsync(reservation, cancellationToken);
                        }

                        // Commit all changes
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        _logger.LogInformation("Successfully cleaned up {TotalCount} expired reservations. Restored stock for {RestoredCount} products",
                            totalCount, restoredCount);

                        return totalCount;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "CleanupExpiredReservations", cancellationToken);

                return totalReservations;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Validation error while cleaning up expired reservations");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while cleaning up expired reservations");
                throw new InvalidOperationException("Unexpected error while cleaning up expired reservations", ex);
            }
        }

        /// <summary>
        /// Bulk retrieves products by their IDs to avoid N+1 query problem
        /// </summary>
        /// <param name="productIds">Collection of product IDs to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping product IDs to product entities for O(1) lookup</returns>
        private async Task<Dictionary<Guid, Domain.Entities.Catalog.Product>> GetProductsByIdsAsync(
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken)
        {
            var uniqueIds = productIds.Distinct().ToList();

            if (uniqueIds.Count == 0)
            {
                return new Dictionary<Guid, Domain.Entities.Catalog.Product>();
            }

            // Use GetFilteredProductsAsync with Contains predicate for true bulk retrieval
            // This generates a single SQL query: WHERE ProductId IN (id1, id2, id3, ...)
            // instead of N separate queries (one per product)
            var productsList = await _unitOfWork.Products.GetFilteredProductsAsync(
                p => uniqueIds.Contains(p.ProductId),
                cancellationToken);

            // Convert to dictionary for O(1) lookup in calling methods
            return productsList.ToDictionary(p => p.ProductId);
        }
    }
}
