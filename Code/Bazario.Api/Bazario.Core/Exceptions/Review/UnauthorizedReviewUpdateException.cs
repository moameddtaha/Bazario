using System;

namespace Bazario.Core.Exceptions.Review
{
    /// <summary>
    /// Exception thrown when a user attempts to update a review they don't own.
    /// </summary>
    public class UnauthorizedReviewUpdateException : Exception
    {
        public Guid ReviewId { get; }
        public Guid RequestingUserId { get; }

        public UnauthorizedReviewUpdateException(Guid reviewId, Guid requestingUserId)
            : base($"User {requestingUserId} is not authorized to update review {reviewId}")
        {
            ReviewId = reviewId;
            RequestingUserId = requestingUserId;
        }

        public UnauthorizedReviewUpdateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
