using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock reservation result
    /// </summary>
    public class StockReservationResult
    {
        public bool IsSuccessful { get; set; }
        public Guid? ReservationId { get; set; }
        public List<ReservationStatus> ItemResults { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
