using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Payment processing result
    /// </summary>
    public class PaymentResult
    {
        public bool IsSuccessful { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal ProcessedAmount { get; set; }
    }
}
