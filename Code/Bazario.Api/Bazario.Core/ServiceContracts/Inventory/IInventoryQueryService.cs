using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory read operations
    /// Handles querying inventory status, history, and reservations
    /// </summary>
    public interface IInventoryQueryService
    {
        /// <summary>
        /// Gets current inventory status for a product
        /// </summary>
        Task<InventoryStatus> GetInventoryStatusAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets products with low stock alerts
        /// </summary>
        Task<List<LowStockAlert>> GetLowStockAlertsAsync(Guid? storeId = null, int threshold = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets inventory movement history for a product
        /// </summary>
        Task<List<InventoryMovement>> GetInventoryHistoryAsync(Guid productId, DateRange? dateRange = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active stock reservations
        /// </summary>
        Task<List<StockReservation>> GetActiveReservationsAsync(Guid? productId = null, Guid? storeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets reservation details by ID
        /// </summary>
        Task<StockReservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets inventory status for multiple products
        /// </summary>
        Task<List<InventoryStatus>> GetInventoryStatusBulkAsync(List<Guid> productIds, CancellationToken cancellationToken = default);
    }
}
