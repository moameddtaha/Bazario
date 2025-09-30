using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory validation and business rules
    /// Handles stock availability checks and inventory constraints
    /// </summary>
    public interface IInventoryValidationService
    {
        /// <summary>
        /// Validates stock availability for multiple products
        /// </summary>
        Task<List<StockValidationResult>> ValidateStockAvailabilityAsync(List<StockCheckItem> stockCheckRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a stock update is allowed
        /// </summary>
        Task<bool> ValidateStockUpdateAsync(Guid productId, int newQuantity, StockUpdateType updateType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a reservation can be created
        /// </summary>
        Task<bool> ValidateReservationAsync(StockReservationRequest reservationRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if product has sufficient stock for quantity
        /// </summary>
        Task<bool> HasSufficientStockAsync(Guid productId, int requiredQuantity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates bulk stock update data
        /// </summary>
        Task<List<BulkUpdateError>> ValidateBulkUpdateAsync(BulkStockUpdateRequest bulkUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if product should trigger low stock alert
        /// </summary>
        Task<bool> ShouldTriggerLowStockAlertAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
