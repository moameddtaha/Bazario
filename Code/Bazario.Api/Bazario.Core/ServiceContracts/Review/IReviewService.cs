using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Review
{
    /// <summary>
    /// Service contract for review management operations
    /// Handles review CRUD, validation, moderation, and analytics
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// Creates a new review with validation and business rules
        /// </summary>
        /// <param name="reviewAddRequest">Review creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created review response</returns>
        /// <exception cref="ArgumentNullException">Thrown when reviewAddRequest is null</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="DuplicateReviewException">Thrown when customer already reviewed this product</exception>
        /// <exception cref="ReviewNotAllowedException">Thrown when customer hasn't purchased the product</exception>
        Task<ReviewResponse> CreateReviewAsync(ReviewAddRequest reviewAddRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing review with validation
        /// </summary>
        /// <param name="reviewUpdateRequest">Review update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated review response</returns>
        /// <exception cref="ArgumentNullException">Thrown when reviewUpdateRequest is null</exception>
        /// <exception cref="ReviewNotFoundException">Thrown when review is not found</exception>
        /// <exception cref="UnauthorizedReviewUpdateException">Thrown when user cannot update this review</exception>
        Task<ReviewResponse> UpdateReviewAsync(ReviewUpdateRequest reviewUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a review if business rules allow
        /// </summary>
        /// <param name="reviewId">Review ID to delete</param>
        /// <param name="requestingUserId">ID of user requesting deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ReviewNotFoundException">Thrown when review is not found</exception>
        /// <exception cref="UnauthorizedReviewDeletionException">Thrown when user cannot delete this review</exception>
        Task<bool> DeleteReviewAsync(Guid reviewId, Guid requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a review by ID
        /// </summary>
        /// <param name="reviewId">Review ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Review response or null if not found</returns>
        Task<ReviewResponse?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all reviews for a specific product with pagination
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="sortBy">Sort criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of product reviews</returns>
        Task<PagedResponse<ReviewResponse>> GetReviewsByProductIdAsync(Guid productId, int pageNumber = 1, int pageSize = 10, ReviewSortBy sortBy = ReviewSortBy.Newest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all reviews by a specific customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer reviews</returns>
        Task<List<ReviewResponse>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive rating statistics for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product rating statistics</returns>
        Task<ProductRatingStats> GetProductRatingStatsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a customer can review a specific product
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Review validation result</returns>
        Task<ReviewValidationResult> ValidateReviewEligibilityAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moderates a review (approve/reject/flag)
        /// </summary>
        /// <param name="reviewId">Review ID</param>
        /// <param name="moderationAction">Moderation action to take</param>
        /// <param name="moderatorId">ID of moderator</param>
        /// <param name="reason">Reason for moderation action</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Moderation result</returns>
        Task<ReviewModerationResult> ModerateReviewAsync(Guid reviewId, ModerationAction moderationAction, Guid moderatorId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets reviews that need moderation
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reviews pending moderation</returns>
        Task<PagedResponse<ReviewResponse>> GetReviewsPendingModerationAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reports a review as inappropriate
        /// </summary>
        /// <param name="reviewId">Review ID to report</param>
        /// <param name="reporterId">ID of user reporting</param>
        /// <param name="reason">Reason for report</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully reported</returns>
        Task<bool> ReportReviewAsync(Guid reviewId, Guid reporterId, string reason, CancellationToken cancellationToken = default);
    }

}
