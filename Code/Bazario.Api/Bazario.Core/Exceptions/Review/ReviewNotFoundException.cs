using System;

namespace Bazario.Core.Exceptions.Review
{
    /// <summary>
    /// Exception thrown when a review cannot be found by ID.
    /// </summary>
    public class ReviewNotFoundException : Exception
    {
        public Guid ReviewId { get; }

        public ReviewNotFoundException(Guid reviewId)
            : base($"Review with ID {reviewId} not found")
        {
            ReviewId = reviewId;
        }

        public ReviewNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
