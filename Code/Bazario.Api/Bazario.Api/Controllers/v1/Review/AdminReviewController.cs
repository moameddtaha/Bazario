using Asp.Versioning;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Review
{
    /// <summary>
    /// Admin API for review moderation and management
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/reviews")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Review Moderation")]
    public class AdminReviewController : ControllerBase
    {
        private readonly IReviewModerationService _reviewModerationService;
        private readonly IReviewManagementService _reviewManagementService;
        private readonly ILogger<AdminReviewController> _logger;

        public AdminReviewController(
            IReviewModerationService reviewModerationService,
            IReviewManagementService reviewManagementService,
            ILogger<AdminReviewController> logger)
        {
            _reviewModerationService = reviewModerationService;
            _reviewManagementService = reviewManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Gets reviews that are pending moderation
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of reviews pending moderation</returns>
        /// <response code="200">Returns reviews pending moderation</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(PagedResponse<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetPendingReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest(new { message = "Page number must be greater than 0" });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { message = "Page size must be between 1 and 100" });
                }

                _logger.LogInformation("Admin fetching pending reviews, Page: {PageNumber}, Size: {PageSize}",
                    pageNumber, pageSize);

                var reviews = await _reviewModerationService.GetReviewsPendingModerationAsync(
                    pageNumber, pageSize, cancellationToken);

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending reviews");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching pending reviews" });
            }
        }

        /// <summary>
        /// Moderates a review (approve, reject, or flag)
        /// </summary>
        /// <param name="reviewId">The review ID</param>
        /// <param name="action">Moderation action (Approve, Reject, Flag)</param>
        /// <param name="reason">Optional reason for the moderation action</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Moderation result</returns>
        /// <response code="200">Review moderated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Review not found</response>
        [HttpPost("{reviewId:guid}/moderate")]
        [ProducesResponseType(typeof(ReviewModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewModerationResult>> ModerateReview(
            Guid reviewId,
            [FromQuery] ModerationAction action,
            [FromBody] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var moderatorId = GetCurrentUserId();

                _logger.LogInformation(
                    "Admin {ModeratorId} moderating review {ReviewId} with action {Action}",
                    moderatorId, reviewId, action);

                var result = await _reviewModerationService.ModerateReviewAsync(
                    reviewId, action, moderatorId, reason, cancellationToken);

                _logger.LogInformation("Review {ReviewId} moderated successfully with action {Action}",
                    reviewId, action);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid moderation request for review: {ReviewId}", reviewId);
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Review not found: {ReviewId}", reviewId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while moderating the review" });
            }
        }

        /// <summary>
        /// Deletes any review (admin override)
        /// </summary>
        /// <param name="reviewId">The review ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Review deleted successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Review not found</response>
        [HttpDelete("{reviewId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteReview(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} deleting review: {ReviewId}", adminId, reviewId);

                // Admin can delete any review, so we pass admin ID as the requesting user
                var result = await _reviewManagementService.DeleteReviewAsync(reviewId, adminId, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Review not found or already deleted" });
                }

                _logger.LogInformation("Review {ReviewId} deleted successfully by admin {AdminId}", reviewId, adminId);

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the review" });
            }
        }

        /// <summary>
        /// Gets all reviews for a specific customer (admin view)
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer's reviews</returns>
        /// <response code="200">Returns the customer's reviews</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("customer/{customerId:guid}")]
        [ProducesResponseType(typeof(List<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ReviewResponse>>> GetCustomerReviews(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching reviews for customer: {CustomerId}", customerId);

                var reviews = await _reviewManagementService.GetReviewsByCustomerIdAsync(customerId, cancellationToken);

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer reviews for: {CustomerId}", customerId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching customer reviews" });
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
