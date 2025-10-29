using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;
using Bazario.Core.Domain.RepositoryContracts.Inventory;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Inventory
{
    public class StockReservationRepository : IStockReservationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockReservationRepository> _logger;

        public StockReservationRepository(ApplicationDbContext context, ILogger<StockReservationRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<StockReservation> AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new stock reservation for product: {ProductId}, customer: {CustomerId}",
                reservation?.ProductId, reservation?.CustomerId);

            try
            {
                // Validate input
                if (reservation == null)
                {
                    _logger.LogWarning("Attempted to add null reservation");
                    throw new ArgumentNullException(nameof(reservation));
                }

                _logger.LogDebug("Adding reservation to database context. ReservationId: {ReservationId}, Quantity: {Quantity}, ExpiresAt: {ExpiresAt}",
                    reservation.ReservationId, reservation.ReservedQuantity, reservation.ExpiresAt);

                // Add reservation to context
                _context.StockReservations.Add(reservation);

                _logger.LogInformation("Successfully added reservation. ReservationId: {ReservationId}, ProductId: {ProductId}, Quantity: {Quantity}",
                    reservation.ReservationId, reservation.ProductId, reservation.ReservedQuantity);

                return Task.FromResult(reservation);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding reservation for product: {ProductId}", reservation?.ProductId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating reservation for product: {ProductId}", reservation?.ProductId);
                throw new InvalidOperationException($"Unexpected error while creating reservation: {ex.Message}", ex);
            }
        }

        public async Task<StockReservation> UpdateReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update reservation: {ReservationId}", reservation?.ReservationId);

            try
            {
                // Validate input
                if (reservation == null)
                {
                    _logger.LogWarning("Attempted to update null reservation");
                    throw new ArgumentNullException(nameof(reservation));
                }

                if (reservation.ReservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update reservation with empty ID");
                    throw new ArgumentException("Reservation ID cannot be empty", nameof(reservation));
                }

                _logger.LogDebug("Checking if reservation exists in database. ReservationId: {ReservationId}", reservation.ReservationId);

                // Check if reservation exists
                var existingReservation = await _context.StockReservations.FindAsync(new object[] { reservation.ReservationId }, cancellationToken);
                if (existingReservation == null)
                {
                    _logger.LogWarning("Reservation not found for update. ReservationId: {ReservationId}", reservation.ReservationId);
                    throw new InvalidOperationException($"Reservation with ID {reservation.ReservationId} not found");
                }

                _logger.LogDebug("Updating reservation properties. ReservationId: {ReservationId}, Status: {Status}, Quantity: {Quantity}",
                    reservation.ReservationId, reservation.Status, reservation.ReservedQuantity);

                // Update properties
                existingReservation.ReservedQuantity = reservation.ReservedQuantity;
                existingReservation.Status = reservation.Status;
                existingReservation.ExpiresAt = reservation.ExpiresAt;
                existingReservation.ConfirmedAt = reservation.ConfirmedAt;
                existingReservation.ReleasedAt = reservation.ReleasedAt;
                existingReservation.OrderId = reservation.OrderId;
                existingReservation.ExternalReference = reservation.ExternalReference;

                _logger.LogInformation("Successfully updated reservation. ReservationId: {ReservationId}, Status: {Status}",
                    reservation.ReservationId, reservation.Status);

                return existingReservation;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating reservation: {ReservationId}", reservation?.ReservationId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating reservation: {ReservationId}", reservation?.ReservationId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating reservation: {ReservationId}", reservation?.ReservationId);
                throw new InvalidOperationException($"Unexpected error while updating reservation with ID {reservation?.ReservationId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> SoftDeleteReservationAsync(Guid reservationId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting soft delete for reservation: {ReservationId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                reservationId, deletedBy, reason);

            try
            {
                // Validate input
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete reservation with empty ID");
                    return false;
                }

                if (deletedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete reservation without valid DeletedBy user ID");
                    return false;
                }

                _logger.LogDebug("Checking if reservation exists for soft deletion. ReservationId: {ReservationId}", reservationId);

                // Find the reservation
                var reservation = await _context.StockReservations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId, cancellationToken);

                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found for soft deletion. ReservationId: {ReservationId}", reservationId);
                    return false;
                }

                if (reservation.IsDeleted)
                {
                    _logger.LogWarning("Reservation is already soft deleted. ReservationId: {ReservationId}", reservationId);
                    return false;
                }

                _logger.LogDebug("Soft deleting reservation. ReservationId: {ReservationId}", reservationId);

                // Set soft delete properties
                reservation.IsDeleted = true;
                reservation.DeletedAt = DateTime.UtcNow;
                reservation.DeletedBy = deletedBy;
                reservation.DeletedReason = reason;

                _logger.LogInformation("Successfully soft deleted reservation. ReservationId: {ReservationId}, DeletedBy: {DeletedBy}",
                    reservationId, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while soft deleting reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while soft deleting reservation with ID {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> RestoreReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting restore for soft deleted reservation: {ReservationId}", reservationId);

            try
            {
                // Validate input
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to restore reservation with empty ID");
                    return false;
                }

                _logger.LogDebug("Checking if soft deleted reservation exists for restore. ReservationId: {ReservationId}", reservationId);

                // Find the reservation including soft deleted ones
                var reservation = await _context.StockReservations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId, cancellationToken);

                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found for restore. ReservationId: {ReservationId}", reservationId);
                    return false;
                }

                if (!reservation.IsDeleted)
                {
                    _logger.LogWarning("Reservation is not soft deleted, cannot restore. ReservationId: {ReservationId}", reservationId);
                    return false;
                }

                _logger.LogDebug("Restoring soft deleted reservation. ReservationId: {ReservationId}", reservationId);

                // Clear soft delete properties
                reservation.IsDeleted = false;
                reservation.DeletedAt = null;
                reservation.DeletedBy = null;
                reservation.DeletedReason = null;

                _logger.LogInformation("Successfully restored reservation. ReservationId: {ReservationId}", reservationId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while restoring reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while restoring reservation with ID {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<StockReservation?> GetReservationByIdIncludeDeletedAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reservation with empty ID");
                    return null;
                }

                _logger.LogDebug("Retrieving reservation including soft deleted. ReservationId: {ReservationId}", reservationId);

                // Query with navigation properties, ignoring soft delete filter
                var reservation = await _context.StockReservations
                    .IgnoreQueryFilters()
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId, cancellationToken);

                if (reservation != null)
                {
                    _logger.LogDebug("Successfully retrieved reservation including deleted. ReservationId: {ReservationId}, IsDeleted: {IsDeleted}",
                        reservationId, reservation.IsDeleted);
                }
                else
                {
                    _logger.LogDebug("Reservation not found. ReservationId: {ReservationId}", reservationId);
                }

                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while retrieving reservation with ID {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting hard delete for reservation: {ReservationId}", reservationId);

            try
            {
                // Validate input
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to hard delete reservation with empty ID");
                    return false;
                }

                _logger.LogDebug("Checking if reservation exists for hard deletion. ReservationId: {ReservationId}", reservationId);

                // Find the reservation (including soft deleted)
                var reservation = await _context.StockReservations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId, cancellationToken);

                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found for hard deletion. ReservationId: {ReservationId}", reservationId);
                    return false;
                }

                _logger.LogDebug("Hard deleting reservation. ReservationId: {ReservationId}", reservationId);

                // Permanently remove from database
                _context.StockReservations.Remove(reservation);

                _logger.LogInformation("Successfully hard deleted reservation. ReservationId: {ReservationId}", reservationId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while hard deleting reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while hard deleting reservation with ID {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<StockReservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (reservationId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reservation with empty ID");
                    return null;
                }

                _logger.LogDebug("Retrieving reservation. ReservationId: {ReservationId}", reservationId);

                // Query with navigation properties (soft delete filter applied automatically)
                var reservation = await _context.StockReservations
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId, cancellationToken);

                if (reservation != null)
                {
                    _logger.LogDebug("Successfully retrieved reservation. ReservationId: {ReservationId}, Status: {Status}",
                        reservationId, reservation.Status);
                }
                else
                {
                    _logger.LogDebug("Reservation not found. ReservationId: {ReservationId}", reservationId);
                }

                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservation: {ReservationId}", reservationId);
                throw new InvalidOperationException($"Unexpected error while retrieving reservation with ID {reservationId}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reservations with empty product ID");
                    return new List<StockReservation>();
                }

                _logger.LogDebug("Retrieving reservations for product. ProductId: {ProductId}", productId);

                var reservations = await _context.StockReservations
                    .Where(r => r.ProductId == productId && r.Status == "Pending")
                    .Include(r => r.Customer)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} active reservations for product: {ProductId}", reservations.Count, productId);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations for product: {ProductId}", productId);
                throw new InvalidOperationException($"Unexpected error while retrieving reservations for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reservations with empty customer ID");
                    return new List<StockReservation>();
                }

                _logger.LogDebug("Retrieving reservations for customer. CustomerId: {CustomerId}", customerId);

                var reservations = await _context.StockReservations
                    .Where(r => r.CustomerId == customerId)
                    .Include(r => r.Product)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} reservations for customer: {CustomerId}", reservations.Count, customerId);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Unexpected error while retrieving reservations for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (orderId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reservations with empty order ID");
                    return new List<StockReservation>();
                }

                _logger.LogDebug("Retrieving reservations for order. OrderId: {OrderId}", orderId);

                var reservations = await _context.StockReservations
                    .Where(r => r.OrderId == orderId)
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} reservations for order: {OrderId}", reservations.Count, orderId);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Unexpected error while retrieving reservations for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(status))
                {
                    _logger.LogWarning("Attempted to retrieve reservations with null or empty status");
                    return new List<StockReservation>();
                }

                _logger.LogDebug("Retrieving reservations by status. Status: {Status}", status);

                var reservations = await _context.StockReservations
                    .Where(r => r.Status == status)
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .OrderBy(r => r.ExpiresAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} reservations with status: {Status}", reservations.Count, status);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations by status: {Status}", status);
                throw new InvalidOperationException($"Unexpected error while retrieving reservations by status {status}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                _logger.LogDebug("Retrieving expired reservations. CurrentTime: {Now}", now);

                var expiredReservations = await _context.StockReservations
                    .Where(r => r.Status == "Pending" && r.ExpiresAt < now)
                    .Include(r => r.Product)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} expired reservations", expiredReservations.Count);

                return expiredReservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving expired reservations");
                throw new InvalidOperationException($"Unexpected error while retrieving expired reservations: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsExpiringBeforeAsync(DateTime expiryTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving reservations expiring before: {ExpiryTime}", expiryTime);

                var reservations = await _context.StockReservations
                    .Where(r => r.Status == "Pending" && r.ExpiresAt < expiryTime)
                    .Include(r => r.Product)
                    .OrderBy(r => r.ExpiresAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} reservations expiring before {ExpiryTime}", reservations.Count, expiryTime);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations expiring before: {ExpiryTime}", expiryTime);
                throw new InvalidOperationException($"Unexpected error while retrieving reservations expiring before {expiryTime}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetFilteredReservationsAsync(Expression<Func<StockReservation, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to get filtered reservations with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Retrieving filtered reservations");

                var reservations = await _context.StockReservations
                    .Where(predicate)
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} filtered reservations", reservations.Count);

                return reservations;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered reservations");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving filtered reservations");
                throw new InvalidOperationException($"Unexpected error while retrieving filtered reservations: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalReservedQuantityByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to get reserved quantity with empty product ID");
                    return 0;
                }

                _logger.LogDebug("Calculating total reserved quantity for product. ProductId: {ProductId}", productId);

                var totalReserved = await _context.StockReservations
                    .Where(r => r.ProductId == productId && r.Status == "Pending")
                    .SumAsync(r => r.ReservedQuantity, cancellationToken);

                _logger.LogInformation("Total reserved quantity for product {ProductId}: {TotalReserved}", productId, totalReserved);

                return totalReserved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while calculating reserved quantity for product: {ProductId}", productId);
                throw new InvalidOperationException($"Unexpected error while calculating reserved quantity for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<Guid, int>> GetBulkReservedQuantitiesAsync(List<Guid> productIds, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productIds == null || productIds.Count == 0)
                {
                    _logger.LogWarning("Attempted to get bulk reserved quantities with null or empty product IDs");
                    return new Dictionary<Guid, int>();
                }

                var uniqueIds = productIds.Distinct().Where(id => id != Guid.Empty).ToList();
                if (uniqueIds.Count == 0)
                {
                    _logger.LogWarning("No valid product IDs provided for bulk reserved quantities");
                    return new Dictionary<Guid, int>();
                }

                _logger.LogDebug("Calculating bulk reserved quantities for {Count} products", uniqueIds.Count);

                // Single query to get all reservations for the specified products
                var reservedQuantities = await _context.StockReservations
                    .Where(r => uniqueIds.Contains(r.ProductId) && r.Status == "Pending")
                    .GroupBy(r => r.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalReserved = g.Sum(r => r.ReservedQuantity) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.TotalReserved, cancellationToken);

                // Ensure all requested products are in the result (with 0 if no reservations)
                var result = uniqueIds.ToDictionary(id => id, id => 0);
                foreach (var kvp in reservedQuantities)
                {
                    result[kvp.Key] = kvp.Value;
                }

                _logger.LogInformation("Retrieved bulk reserved quantities for {Count} products", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while calculating bulk reserved quantities");
                throw new InvalidOperationException($"Unexpected error while calculating bulk reserved quantities: {ex.Message}", ex);
            }
        }

        public async Task<int> GetActiveReservationCountByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to get active reservation count with empty customer ID");
                    return 0;
                }

                _logger.LogDebug("Counting active reservations for customer. CustomerId: {CustomerId}", customerId);

                var count = await _context.StockReservations
                    .CountAsync(r => r.CustomerId == customerId && r.Status == "Pending", cancellationToken);

                _logger.LogInformation("Active reservation count for customer {CustomerId}: {Count}", customerId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while counting active reservations for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Unexpected error while counting active reservations for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<StockReservation>> GetReservationsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving reservations by date range. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);

                var reservations = await _context.StockReservations
                    .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                    .Include(r => r.Product)
                    .Include(r => r.Customer)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} reservations between {StartDate} and {EndDate}",
                    reservations.Count, startDate, endDate);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving reservations by date range");
                throw new InvalidOperationException($"Unexpected error while retrieving reservations by date range: {ex.Message}", ex);
            }
        }
    }
}