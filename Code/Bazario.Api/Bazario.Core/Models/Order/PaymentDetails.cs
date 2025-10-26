using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Payment details for processing
    /// </summary>
    public class PaymentDetails
    {
        public string? PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string? CardToken { get; set; }
        public string? Currency { get; set; } = "EGP"; // Egyptian Pound (Egypt focus)
    }
}
