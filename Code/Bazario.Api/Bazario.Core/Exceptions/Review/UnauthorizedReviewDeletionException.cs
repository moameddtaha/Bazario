using System;

namespace Bazario.Core.Exceptions.Review
{
    /// <summary>
    /// Exception thrown when a user attempts to delete a review they don't own.
    /// </summary>
    public class UnauthorizedReviewDeletionException : Exception
    {
        public Guid ReviewId { get; }
        public Guid RequestingUserId { get; }

        public UnauthorizedReviewDeletionException(Guid reviewId, Guid requestingUserId)
            : base($"User {requestingUserId} is not authorized to delete review {reviewId}")
        {
            ReviewId = reviewId;
            RequestingUserId = requestingUserId;
        }

        public UnauthorizedReviewDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
