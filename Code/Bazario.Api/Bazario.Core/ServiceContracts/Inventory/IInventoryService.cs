using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Inventory;
using Bazario.Core.Models.Store;

namespace Bazario.Core.ServiceContracts
{
    /// <summary>
    /// Service contract for inventory management operations
    /// Handles stock tracking, reservations, and inventory analytics
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Updates product stock with audit trail
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="newQuantity">New stock quantity</param>
        /// <param name="updateType">Type of stock update</param>
        /// <param name="reason">Reason for update</param>
        /// <param name="updatedBy">User performing update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated inventory status</returns>
        Task<InventoryUpdateResult> UpdateStockAsync(Guid productId, int newQuantity, StockUpdateType updateType, string reason, Guid updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reserves stock for an order
        /// </summary>
        /// <param name="reservationRequest">Stock reservation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reservation result</returns>
        Task<StockReservationResult> ReserveStockAsync(StockReservationRequest reservationRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases reserved stock
        /// </summary>
        /// <param name="reservationId">Reservation ID to release</param>
        /// <param name="reason">Reason for release</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully released</returns>
        Task<bool> ReleaseReservationAsync(Guid reservationId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms reserved stock (converts to actual sale)
        /// </summary>
        /// <param name="reservationId">Reservation ID to confirm</param>
        /// <param name="orderId">Order ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully confirmed</returns>
        Task<bool> ConfirmReservationAsync(Guid reservationId, Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current inventory status for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current inventory status</returns>
        Task<InventoryStatus> GetInventoryStatusAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets products with low stock alerts
        /// </summary>
        /// <param name="storeId">Store ID (optional)</param>
        /// <param name="threshold">Stock threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Products with low stock</returns>
        Task<List<LowStockAlert>> GetLowStockAlertsAsync(Guid? storeId = null, int threshold = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets inventory movement history for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="dateRange">Date range for history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Inventory movement history</returns>
        Task<List<InventoryMovement>> GetInventoryHistoryAsync(Guid productId, DateRange? dateRange = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs bulk stock update from file/import
        /// </summary>
        /// <param name="bulkUpdateRequest">Bulk update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bulk update result</returns>
        Task<BulkInventoryUpdateResult> BulkUpdateStockAsync(BulkStockUpdateRequest bulkUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates inventory report for a store or date range
        /// </summary>
        /// <param name="reportRequest">Report parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Inventory report data</returns>
        Task<InventoryReport> GenerateInventoryReportAsync(InventoryReportRequest reportRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active stock reservations
        /// </summary>
        /// <param name="productId">Product ID (optional)</param>
        /// <param name="storeId">Store ID (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Active reservations</returns>
        Task<List<StockReservation>> GetActiveReservationsAsync(Guid? productId = null, Guid? storeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up expired reservations
        /// </summary>
        /// <param name="expirationMinutes">Minutes after which reservations expire</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of reservations cleaned up</returns>
        Task<int> CleanupExpiredReservationsAsync(int expirationMinutes = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets low stock threshold for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="threshold">New threshold value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully set</returns>
        Task<bool> SetLowStockThresholdAsync(Guid productId, int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates stock availability for multiple products
        /// </summary>
        /// <param name="stockCheckRequest">Products and quantities to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stock validation results</returns>
        Task<List<StockValidationResult>> ValidateStockAvailabilityAsync(List<StockCheckItem> stockCheckRequest, CancellationToken cancellationToken = default);
    }
}
