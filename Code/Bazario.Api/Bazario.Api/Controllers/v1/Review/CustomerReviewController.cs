using Asp.Versioning;
using Bazario.Core.DTO.Review;
using Bazario.Core.Exceptions.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Review
{
    /// <summary>
    /// Customer API for managing their own product reviews
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customer/reviews")]
    [Authorize(Roles = "Customer")]
    [Tags("Customer - Reviews")]
    public class CustomerReviewController : ControllerBase
    {
        private readonly IReviewManagementService _reviewManagementService;
        private readonly IReviewValidationService _reviewValidationService;
        private readonly IReviewModerationService _reviewModerationService;
        private readonly ILogger<CustomerReviewController> _logger;

        public CustomerReviewController(
            IReviewManagementService reviewManagementService,
            IReviewValidationService reviewValidationService,
            IReviewModerationService reviewModerationService,
            ILogger<CustomerReviewController> logger)
        {
            _reviewManagementService = reviewManagementService;
            _reviewValidationService = reviewValidationService;
            _reviewModerationService = reviewModerationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all reviews created by the current customer
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer's reviews</returns>
        /// <response code="200">Returns the customer's reviews</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("my-reviews")]
        [ProducesResponseType(typeof(List<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<ReviewResponse>>> GetMyReviews(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();
                _logger.LogInformation("Fetching reviews for customer: {CustomerId}", customerId);

                var reviews = await _reviewManagementService.GetReviewsByCustomerIdAsync(customerId, cancellationToken);

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer reviews");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching your reviews" });
            }
        }

        /// <summary>
        /// Checks if the current customer can review a specific product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Review eligibility status</returns>
        /// <response code="200">Returns eligibility status</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("can-review/{productId:guid}")]
        [ProducesResponseType(typeof(ReviewValidationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ReviewValidationResult>> CanReviewProduct(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();
                _logger.LogInformation("Checking review eligibility for customer: {CustomerId}, Product: {ProductId}",
                    customerId, productId);

                var validationResult = await _reviewValidationService.ValidateReviewEligibilityAsync(
                    customerId, productId, cancellationToken);

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking review eligibility for product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while checking review eligibility" });
            }
        }

        /// <summary>
        /// Creates a new review for a product
        /// </summary>
        /// <param name="request">Review creation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created review</returns>
        /// <response code="201">Review created successfully</response>
        /// <response code="400">Invalid request or validation failed</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="409">Customer already reviewed this product</response>
        [HttpPost]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReviewResponse>> CreateReview(
            [FromBody] ReviewAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();

                if (request.CustomerId != customerId)
                {
                    _logger.LogWarning("Customer ID mismatch. Token: {TokenId}, Request: {RequestId}",
                        customerId, request.CustomerId);
                    return BadRequest(new { message = "You can only create reviews for yourself" });
                }

                _logger.LogInformation("Creating review for customer: {CustomerId}, Product: {ProductId}",
                    customerId, request.ProductId);

                var review = await _reviewManagementService.CreateReviewAsync(request, cancellationToken);

                _logger.LogInformation("Review created successfully: {ReviewId}", review.ReviewId);

                return CreatedAtAction(
                    nameof(PublicReviewController.GetReviewById),
                    "PublicReview",
                    new { reviewId = review.ReviewId, version = "1.0" },
                    review);
            }
            catch (DuplicateReviewException ex)
            {
                _logger.LogWarning(ex, "Duplicate review attempt for product: {ProductId}", request.ProductId);
                return Conflict(new { message = ex.Message });
            }
            catch (ReviewNotAllowedException ex)
            {
                _logger.LogWarning(ex, "Review not allowed for product: {ProductId}", request.ProductId);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid review creation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the review" });
            }
        }

        /// <summary>
        /// Updates an existing review
        /// </summary>
        /// <param name="reviewId">The review ID</param>
        /// <param name="request">Review update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated review</returns>
        /// <response code="200">Review updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to update this review</response>
        /// <response code="404">Review not found</response>
        [HttpPut("{reviewId:guid}")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewResponse>> UpdateReview(
            Guid reviewId,
            [FromBody] ReviewUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();

                if (request.ReviewId != reviewId)
                {
                    return BadRequest(new { message = "Review ID in URL and body must match" });
                }

                if (request.RequestingUserId != customerId)
                {
                    _logger.LogWarning("User ID mismatch. Token: {TokenId}, Request: {RequestId}",
                        customerId, request.RequestingUserId);
                    return BadRequest(new { message = "You can only update your own reviews" });
                }

                _logger.LogInformation("Updating review: {ReviewId} by customer: {CustomerId}", reviewId, customerId);

                var review = await _reviewManagementService.UpdateReviewAsync(request, cancellationToken);

                _logger.LogInformation("Review updated successfully: {ReviewId}", reviewId);

                return Ok(review);
            }
            catch (ReviewNotFoundException ex)
            {
                _logger.LogWarning(ex, "Review not found: {ReviewId}", reviewId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedReviewUpdateException ex)
            {
                _logger.LogWarning(ex, "Unauthorized review update attempt: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid review update request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the review" });
            }
        }

        /// <summary>
        /// Deletes a review
        /// </summary>
        /// <param name="reviewId">The review ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Review deleted successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to delete this review</response>
        /// <response code="404">Review not found</response>
        [HttpDelete("{reviewId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteReview(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();

                _logger.LogInformation("Deleting review: {ReviewId} by customer: {CustomerId}", reviewId, customerId);

                var result = await _reviewManagementService.DeleteReviewAsync(reviewId, customerId, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Review not found or already deleted" });
                }

                _logger.LogInformation("Review deleted successfully: {ReviewId}", reviewId);

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (ReviewNotFoundException ex)
            {
                _logger.LogWarning(ex, "Review not found: {ReviewId}", reviewId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedReviewDeletionException ex)
            {
                _logger.LogWarning(ex, "Unauthorized review deletion attempt: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the review" });
            }
        }

        /// <summary>
        /// Reports a review as inappropriate
        /// </summary>
        /// <param name="reviewId">The review ID to report</param>
        /// <param name="reason">Reason for reporting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Review reported successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">Review not found</response>
        [HttpPost("{reviewId:guid}/report")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ReportReview(
            Guid reviewId,
            [FromBody] string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(new { message = "Report reason is required" });
                }

                var customerId = GetCurrentUserId();

                _logger.LogInformation("Customer {CustomerId} reporting review: {ReviewId}", customerId, reviewId);

                var result = await _reviewModerationService.ReportReviewAsync(
                    reviewId, customerId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Review not found" });
                }

                _logger.LogInformation("Review reported successfully: {ReviewId}", reviewId);

                return Ok(new { message = "Review reported successfully and will be reviewed by moderators" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while reporting the review" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }
    }
}
