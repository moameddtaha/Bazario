using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Reservation status for individual item
    /// </summary>
    public class ReservationStatus
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsFullyReserved { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
