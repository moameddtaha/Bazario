using System;

namespace Bazario.Core.Exceptions.Review
{
    /// <summary>
    /// Exception thrown when a customer attempts to review a product they've already reviewed.
    /// </summary>
    public class DuplicateReviewException : Exception
    {
        public Guid CustomerId { get; }
        public Guid ProductId { get; }

        public DuplicateReviewException(Guid customerId, Guid productId)
            : base($"Customer {customerId} has already reviewed product {productId}")
        {
            CustomerId = customerId;
            ProductId = productId;
        }

        public DuplicateReviewException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
