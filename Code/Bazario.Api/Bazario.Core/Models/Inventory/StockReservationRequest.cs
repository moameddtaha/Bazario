using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock reservation request
    /// </summary>
    public class StockReservationRequest
    {
        public List<ReservationItem> Items { get; set; } = new();
        public Guid CustomerId { get; set; }
        public string? OrderReference { get; set; }
        public int ExpirationMinutes { get; set; } = 30;
    }
}
