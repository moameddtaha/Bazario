using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock reservation
    /// </summary>
    public class StockReservation
    {
        public Guid ReservationId { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public int ReservedQuantity { get; set; }
        public Guid CustomerId { get; set; }
        public string? OrderReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Row version for optimistic concurrency control
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
