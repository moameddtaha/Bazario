using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;

namespace Bazario.Core.Domain.RepositoryContracts.Inventory
{
    /// <summary>
    /// Repository contract for managing stock reservations
    /// </summary>
    public interface IStockReservationRepository
    {
        /// <summary>
        /// Creates a new stock reservation
        /// </summary>
        Task<StockReservation> AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing stock reservation
        /// </summary>
        Task<StockReservation> UpdateReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a reservation by setting IsDeleted = true
        /// </summary>
        Task<bool> SoftDeleteReservationAsync(Guid reservationId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted reservation by setting IsDeleted = false
        /// </summary>
        Task<bool> RestoreReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a reservation by ID including soft-deleted reservations (ignores query filter)
        /// </summary>
        Task<StockReservation?> GetReservationByIdIncludeDeletedAsync(Guid reservationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hard deletes a reservation (permanently removes from database)
        /// </summary>
        Task<bool> DeleteReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a reservation by ID (excludes soft-deleted)
        /// </summary>
        Task<StockReservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active reservations for a specific product
        /// </summary>
        Task<List<StockReservation>> GetReservationsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all reservations for a specific customer
        /// </summary>
        Task<List<StockReservation>> GetReservationsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all reservations linked to a specific order
        /// </summary>
        Task<List<StockReservation>> GetReservationsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets reservations by status (Pending, Confirmed, Released, Expired)
        /// </summary>
        Task<List<StockReservation>> GetReservationsByStatusAsync(string status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all expired reservations that need cleanup
        /// </summary>
        Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets reservations expiring within a specific timeframe
        /// </summary>
        Task<List<StockReservation>> GetReservationsExpiringBeforeAsync(DateTime expiryTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets filtered reservations based on custom predicate
        /// </summary>
        Task<List<StockReservation>> GetFilteredReservationsAsync(Expression<Func<StockReservation, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total reserved quantity for a specific product
        /// </summary>
        Task<int> GetTotalReservedQuantityByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total reserved quantities for multiple products in a single query
        /// </summary>
        Task<Dictionary<Guid, int>> GetBulkReservedQuantitiesAsync(List<Guid> productIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets count of active reservations for a customer
        /// </summary>
        Task<int> GetActiveReservationCountByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all reservations created within a date range
        /// </summary>
        Task<List<StockReservation>> GetReservationsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
