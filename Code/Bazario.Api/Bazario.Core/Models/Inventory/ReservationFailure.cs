using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Details of a failed reservation attempt
    /// </summary>
    public class ReservationFailure
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
