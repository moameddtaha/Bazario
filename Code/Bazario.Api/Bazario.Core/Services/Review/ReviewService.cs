using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Review
{
    /// <summary>
    /// Composite service for review operations.
    /// Delegates to specialized services for separation of concerns.
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly IReviewManagementService _managementService;
        private readonly IReviewValidationService _validationService;
        private readonly IReviewAnalyticsService _analyticsService;
        private readonly IReviewModerationService _moderationService;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewManagementService managementService,
            IReviewValidationService validationService,
            IReviewAnalyticsService analyticsService,
            IReviewModerationService moderationService,
            ILogger<ReviewService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Delegate all methods to specialized services

        public Task<ReviewResponse> CreateReviewAsync(ReviewAddRequest reviewAddRequest, CancellationToken cancellationToken = default)
            => _managementService.CreateReviewAsync(reviewAddRequest, cancellationToken);

        public Task<ReviewResponse> UpdateReviewAsync(ReviewUpdateRequest reviewUpdateRequest, CancellationToken cancellationToken = default)
            => _managementService.UpdateReviewAsync(reviewUpdateRequest, cancellationToken);

        public Task<bool> DeleteReviewAsync(Guid reviewId, Guid requestingUserId, CancellationToken cancellationToken = default)
            => _managementService.DeleteReviewAsync(reviewId, requestingUserId, cancellationToken);

        public Task<ReviewResponse?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
            => _managementService.GetReviewByIdAsync(reviewId, cancellationToken);

        public Task<PagedResponse<ReviewResponse>> GetReviewsByProductIdAsync(Guid productId, int pageNumber = 1, int pageSize = 10, ReviewSortBy sortBy = ReviewSortBy.Newest, CancellationToken cancellationToken = default)
            => _managementService.GetReviewsByProductIdAsync(productId, pageNumber, pageSize, sortBy, cancellationToken);

        public Task<List<ReviewResponse>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
            => _managementService.GetReviewsByCustomerIdAsync(customerId, cancellationToken);

        public Task<ProductRatingStats> GetProductRatingStatsAsync(Guid productId, CancellationToken cancellationToken = default)
            => _analyticsService.GetProductRatingStatsAsync(productId, cancellationToken);

        public Task<ReviewValidationResult> ValidateReviewEligibilityAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => _validationService.ValidateReviewEligibilityAsync(customerId, productId, cancellationToken);

        public Task<ReviewModerationResult> ModerateReviewAsync(Guid reviewId, ModerationAction moderationAction, Guid moderatorId, string? reason = null, CancellationToken cancellationToken = default)
            => _moderationService.ModerateReviewAsync(reviewId, moderationAction, moderatorId, reason, cancellationToken);

        public Task<PagedResponse<ReviewResponse>> GetReviewsPendingModerationAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
            => _moderationService.GetReviewsPendingModerationAsync(pageNumber, pageSize, cancellationToken);

        public Task<bool> ReportReviewAsync(Guid reviewId, Guid reporterId, string reason, CancellationToken cancellationToken = default)
            => _moderationService.ReportReviewAsync(reviewId, reporterId, reason, cancellationToken);
    }
}
