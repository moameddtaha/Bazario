using System;

namespace Bazario.Core.Exceptions.Review
{
    /// <summary>
    /// Exception thrown when a customer is not allowed to review a product (e.g., hasn't purchased it).
    /// </summary>
    public class ReviewNotAllowedException : Exception
    {
        public Guid CustomerId { get; }
        public Guid ProductId { get; }
        public string Reason { get; }

        public ReviewNotAllowedException(Guid customerId, Guid productId, string reason)
            : base($"Customer {customerId} cannot review product {productId}: {reason}")
        {
            CustomerId = customerId;
            ProductId = productId;
            Reason = reason;
        }

        public ReviewNotAllowedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
