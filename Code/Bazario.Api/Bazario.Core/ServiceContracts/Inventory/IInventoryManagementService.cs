using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory CRUD operations
    /// Handles stock updates, reservations, and basic inventory management
    /// </summary>
    public interface IInventoryManagementService
    {
        /// <summary>
        /// Updates product stock with audit trail
        /// </summary>
        Task<InventoryUpdateResult> UpdateStockAsync(Guid productId, int newQuantity, StockUpdateType updateType, string reason, Guid updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reserves stock for an order
        /// </summary>
        Task<StockReservationResult> ReserveStockAsync(StockReservationRequest reservationRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases reserved stock
        /// </summary>
        Task<bool> ReleaseReservationAsync(Guid reservationId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms reserved stock (converts to actual sale)
        /// </summary>
        Task<bool> ConfirmReservationAsync(Guid reservationId, Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs bulk stock update from file/import
        /// </summary>
        Task<BulkInventoryUpdateResult> BulkUpdateStockAsync(BulkStockUpdateRequest bulkUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets low stock threshold for a product
        /// </summary>
        Task<bool> SetLowStockThresholdAsync(Guid productId, int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up expired reservations
        /// </summary>
        Task<int> CleanupExpiredReservationsAsync(int expirationMinutes = 30, CancellationToken cancellationToken = default);
    }
}
