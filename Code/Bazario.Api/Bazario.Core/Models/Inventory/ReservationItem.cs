using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Reservation item
    /// </summary>
    public class ReservationItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
