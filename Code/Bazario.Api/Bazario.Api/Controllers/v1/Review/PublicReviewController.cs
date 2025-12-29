using Asp.Versioning;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Review
{
    /// <summary>
    /// Public API for viewing product reviews and ratings (no authentication required)
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/reviews")]
    [Tags("Public - Reviews")]
    public class PublicReviewController : ControllerBase
    {
        private readonly IReviewManagementService _reviewManagementService;
        private readonly IReviewAnalyticsService _reviewAnalyticsService;
        private readonly ILogger<PublicReviewController> _logger;

        public PublicReviewController(
            IReviewManagementService reviewManagementService,
            IReviewAnalyticsService reviewAnalyticsService,
            ILogger<PublicReviewController> logger)
        {
            _reviewManagementService = reviewManagementService;
            _reviewAnalyticsService = reviewAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a specific review by ID
        /// </summary>
        /// <param name="reviewId">The review ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Review details</returns>
        /// <response code="200">Returns the review</response>
        /// <response code="404">Review not found</response>
        [HttpGet("{reviewId:guid}")]
        [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewResponse>> GetReviewById(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching review: {ReviewId}", reviewId);

                var review = await _reviewManagementService.GetReviewByIdAsync(reviewId, cancellationToken);

                if (review == null)
                {
                    _logger.LogWarning("Review not found: {ReviewId}", reviewId);
                    return NotFound(new { message = "Review not found" });
                }

                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review: {ReviewId}", reviewId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the review" });
            }
        }

        /// <summary>
        /// Gets all reviews for a specific product with pagination and sorting
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 50)</param>
        /// <param name="sortBy">Sort order (Newest, Oldest, HighestRating, LowestRating)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of product reviews</returns>
        /// <response code="200">Returns the paginated reviews</response>
        /// <response code="400">Invalid parameters</response>
        [HttpGet("product/{productId:guid}")]
        [ProducesResponseType(typeof(PagedResponse<ReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetProductReviews(
            Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] ReviewSortBy sortBy = ReviewSortBy.Newest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest(new { message = "Page number must be greater than 0" });
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new { message = "Page size must be between 1 and 50" });
                }

                _logger.LogInformation(
                    "Fetching reviews for product: {ProductId}, Page: {PageNumber}, Size: {PageSize}, Sort: {SortBy}",
                    productId, pageNumber, pageSize, sortBy);

                var reviews = await _reviewManagementService.GetReviewsByProductIdAsync(
                    productId, pageNumber, pageSize, sortBy, cancellationToken);

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product reviews: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching product reviews" });
            }
        }

        /// <summary>
        /// Gets comprehensive rating statistics for a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product rating statistics including average rating, rating distribution, and total reviews</returns>
        /// <response code="200">Returns the rating statistics</response>
        /// <response code="404">Product not found</response>
        [HttpGet("product/{productId:guid}/stats")]
        [ProducesResponseType(typeof(ProductRatingStats), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductRatingStats>> GetProductRatingStats(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching rating stats for product: {ProductId}", productId);

                var stats = await _reviewAnalyticsService.GetProductRatingStatsAsync(productId, cancellationToken);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product rating stats: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching rating statistics" });
            }
        }
    }
}
