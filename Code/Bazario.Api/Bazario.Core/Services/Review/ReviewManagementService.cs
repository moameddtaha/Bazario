using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Review;
using Bazario.Core.Exceptions.Review;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Review
{
    /// <summary>
    /// Service for managing review CRUD operations
    /// </summary>
    public class ReviewManagementService : IReviewManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReviewValidationService _validationService;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ReviewManagementService> _logger;

        public ReviewManagementService(
            IUnitOfWork unitOfWork,
            IReviewValidationService validationService,
            IConcurrencyHelper concurrencyHelper,
            ILogger<ReviewManagementService> logger,
            TimeProvider? timeProvider = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        /// Creates a new review with validation and business rules
        /// </summary>
        public async Task<ReviewResponse> CreateReviewAsync(
            ReviewAddRequest reviewAddRequest,
            CancellationToken cancellationToken = default)
        {
            if (reviewAddRequest == null)
            {
                throw new ArgumentNullException(nameof(reviewAddRequest));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Creating review for customer {CustomerId} and product {ProductId}",
                reviewAddRequest.CustomerId, reviewAddRequest.ProductId);

            // Validate review eligibility
            var validationResult = await _validationService.ValidateReviewEligibilityAsync(
                reviewAddRequest.CustomerId,
                reviewAddRequest.ProductId,
                cancellationToken);

            if (!validationResult.CanReview)
            {
                if (validationResult.AlreadyReviewed)
                {
                    throw new DuplicateReviewException(reviewAddRequest.CustomerId, reviewAddRequest.ProductId);
                }

                if (!validationResult.HasPurchased)
                {
                    throw new ReviewNotAllowedException(
                        reviewAddRequest.CustomerId,
                        reviewAddRequest.ProductId,
                        "You must purchase this product before reviewing it");
                }

                // Generic validation failure
                throw new ReviewNotAllowedException(
                    reviewAddRequest.CustomerId,
                    reviewAddRequest.ProductId,
                    string.Join(", ", validationResult.ValidationMessages));
            }

            // Create review entity
            var review = reviewAddRequest.ToReview();
            review.ReviewId = Guid.NewGuid();
            review.CreatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            var createdReview = await _unitOfWork.Reviews.AddReviewAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Review created successfully with ID: {ReviewId} in {ElapsedMs}ms",
                createdReview.ReviewId,
                stopwatch.ElapsedMilliseconds);

            return ReviewResponse.FromReview(createdReview);
        }

        /// <summary>
        /// Updates an existing review with validation
        /// </summary>
        public async Task<ReviewResponse> UpdateReviewAsync(
            ReviewUpdateRequest reviewUpdateRequest,
            CancellationToken cancellationToken = default)
        {
            if (reviewUpdateRequest == null)
            {
                throw new ArgumentNullException(nameof(reviewUpdateRequest));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Updating review {ReviewId}", reviewUpdateRequest.ReviewId);

            var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
            {
                var existingReview = await _unitOfWork.Reviews.GetReviewByIdAsync(reviewUpdateRequest.ReviewId, cancellationToken);

                if (existingReview == null)
                {
                    _logger.LogWarning("Review not found with ID: {ReviewId}", reviewUpdateRequest.ReviewId);
                    throw new ReviewNotFoundException(reviewUpdateRequest.ReviewId);
                }

                // Verify requesting user is the review author
                if (existingReview.CustomerId != reviewUpdateRequest.RequestingUserId)
                {
                    _logger.LogWarning(
                        "User {RequestingUserId} attempted to update review {ReviewId} owned by {CustomerId}",
                        reviewUpdateRequest.RequestingUserId, reviewUpdateRequest.ReviewId, existingReview.CustomerId);
                    throw new UnauthorizedReviewUpdateException(reviewUpdateRequest.ReviewId, reviewUpdateRequest.RequestingUserId);
                }

                // Update fields
                if (reviewUpdateRequest.Rating.HasValue)
                {
                    if (!_validationService.ValidateRating(reviewUpdateRequest.Rating.Value))
                    {
                        throw new ArgumentException("Rating must be between 1 and 5", nameof(reviewUpdateRequest.Rating));
                    }
                    existingReview.Rating = reviewUpdateRequest.Rating.Value;
                }

                if (reviewUpdateRequest.Comment != null)
                {
                    existingReview.Comment = reviewUpdateRequest.Comment;
                }

                var updatedReview = await _unitOfWork.Reviews.UpdateReviewAsync(existingReview, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return updatedReview;
            }, "UpdateReview", cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Review updated successfully with ID: {ReviewId} in {ElapsedMs}ms",
                result.ReviewId,
                stopwatch.ElapsedMilliseconds);

            return ReviewResponse.FromReview(result);
        }

        /// <summary>
        /// Deletes a review if business rules allow
        /// </summary>
        public async Task<bool> DeleteReviewAsync(
            Guid reviewId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            if (reviewId == Guid.Empty)
            {
                _logger.LogWarning("DeleteReviewAsync called with empty review ID");
                throw new ArgumentException("Review ID cannot be empty", nameof(reviewId));
            }

            if (requestingUserId == Guid.Empty)
            {
                _logger.LogWarning("DeleteReviewAsync called with empty requesting user ID");
                throw new ArgumentException("Requesting user ID cannot be empty", nameof(requestingUserId));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Deleting review {ReviewId}", reviewId);

            var existingReview = await _unitOfWork.Reviews.GetReviewByIdAsync(reviewId, cancellationToken);

            if (existingReview == null)
            {
                _logger.LogWarning("Review not found with ID: {ReviewId}", reviewId);
                throw new ReviewNotFoundException(reviewId);
            }

            // Verify requesting user is the review author
            if (existingReview.CustomerId != requestingUserId)
            {
                _logger.LogWarning(
                    "User {RequestingUserId} attempted to delete review {ReviewId} owned by {CustomerId}",
                    requestingUserId, reviewId, existingReview.CustomerId);
                throw new UnauthorizedReviewDeletionException(reviewId, requestingUserId);
            }

            var result = await _unitOfWork.Reviews.DeleteReviewByIdAsync(reviewId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Review deleted successfully with ID: {ReviewId} in {ElapsedMs}ms",
                reviewId,
                stopwatch.ElapsedMilliseconds);

            return result;
        }

        /// <summary>
        /// Retrieves a review by ID
        /// </summary>
        public async Task<ReviewResponse?> GetReviewByIdAsync(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            if (reviewId == Guid.Empty)
            {
                _logger.LogWarning("GetReviewByIdAsync called with empty review ID");
                throw new ArgumentException("Review ID cannot be empty", nameof(reviewId));
            }

            _logger.LogDebug("Getting review by ID: {ReviewId}", reviewId);
            var review = await _unitOfWork.Reviews.GetReviewByIdAsync(reviewId, cancellationToken);
            return review != null ? ReviewResponse.FromReview(review) : null;
        }

        /// <summary>
        /// Retrieves all reviews for a specific product with pagination
        /// </summary>
        public async Task<PagedResponse<ReviewResponse>> GetReviewsByProductIdAsync(
            Guid productId,
            int pageNumber = 1,
            int pageSize = 10,
            ReviewSortBy sortBy = ReviewSortBy.Newest,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("GetReviewsByProductIdAsync called with empty product ID");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100");
            }

            _logger.LogDebug("Getting reviews for product {ProductId} (page {Page}, size {Size}, sort {Sort})",
                productId, pageNumber, pageSize, sortBy);

            var allReviews = await _unitOfWork.Reviews.GetReviewsByProductIdAsync(productId, cancellationToken);

            // Apply sorting
            IEnumerable<Domain.Entities.Review.Review> sortedReviews = sortBy switch
            {
                ReviewSortBy.Newest => allReviews.OrderByDescending(r => r.CreatedAt),
                ReviewSortBy.Oldest => allReviews.OrderBy(r => r.CreatedAt),
                ReviewSortBy.HighestRating => allReviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                ReviewSortBy.LowestRating => allReviews.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                ReviewSortBy.MostHelpful => throw new NotImplementedException(
                    "MostHelpful sorting requires HelpfulCount field in Review entity (not yet implemented)"),
                _ => allReviews.OrderByDescending(r => r.CreatedAt)
            };

            var totalCount = sortedReviews.Count();
            var pagedReviews = sortedReviews
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ReviewResponse.FromReview)
                .ToList();

            return new PagedResponse<ReviewResponse>
            {
                Items = pagedReviews,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Retrieves all reviews by a specific customer
        /// </summary>
        public async Task<List<ReviewResponse>> GetReviewsByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            if (customerId == Guid.Empty)
            {
                _logger.LogWarning("GetReviewsByCustomerIdAsync called with empty customer ID");
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
            }

            _logger.LogDebug("Getting reviews by customer ID: {CustomerId}", customerId);
            var reviews = await _unitOfWork.Reviews.GetReviewsByCustomerIdAsync(customerId, cancellationToken);
            return reviews.Select(ReviewResponse.FromReview).ToList();
        }
    }
}
