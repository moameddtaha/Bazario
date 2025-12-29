using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Discount
{
    /// <summary>
    /// Admin API for managing all discounts
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/discounts")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Discounts")]
    public class AdminDiscountController : ControllerBase
    {
        private readonly IDiscountManagementService _discountManagementService;
        private readonly IDiscountAnalyticsService _discountAnalyticsService;
        private readonly ILogger<AdminDiscountController> _logger;

        public AdminDiscountController(
            IDiscountManagementService discountManagementService,
            IDiscountAnalyticsService discountAnalyticsService,
            ILogger<AdminDiscountController> logger)
        {
            _discountManagementService = discountManagementService;
            _discountAnalyticsService = discountAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active discounts with pagination
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetActiveDiscounts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1 || pageSize > 1000)
                {
                    return BadRequest(new { message = "Invalid page parameters" });
                }

                var discounts = await _discountManagementService.GetActiveDiscountsAsync(
                    pageNumber, pageSize, cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching discounts" });
            }
        }

        /// <summary>
        /// Gets all global discounts
        /// </summary>
        [HttpGet("global")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetGlobalDiscounts(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var discounts = await _discountManagementService.GetGlobalDiscountsAsync(cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching global discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching global discounts" });
            }
        }

        /// <summary>
        /// Gets discounts by type
        /// </summary>
        [HttpGet("type/{type}")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetDiscountsByType(
            DiscountType type,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var discounts = await _discountManagementService.GetDiscountsByTypeAsync(type, cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discounts by type: {Type}", type);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching discounts" });
            }
        }

        /// <summary>
        /// Gets expiring discounts
        /// </summary>
        [HttpGet("expiring")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetExpiringDiscounts(
            [FromQuery] int daysUntilExpiry = 7,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var discounts = await _discountManagementService.GetExpiringDiscountsAsync(
                    daysUntilExpiry, cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expiring discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching expiring discounts" });
            }
        }

        /// <summary>
        /// Gets discount by code
        /// </summary>
        [HttpGet("code/{code}")]
        [ProducesResponseType(typeof(DiscountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DiscountResponse>> GetDiscountByCode(
            string code,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var discount = await _discountManagementService.GetDiscountByCodeAsync(code, cancellationToken);

                if (discount == null)
                {
                    return NotFound(new { message = "Discount not found" });
                }

                return Ok(discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount by code: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the discount" });
            }
        }

        /// <summary>
        /// Gets overall discount statistics
        /// </summary>
        [HttpGet("analytics/overall")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetOverallDiscountStats(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _discountAnalyticsService.GetOverallDiscountStatsAsync(cancellationToken);

                return Ok(new
                {
                    totalCreated = stats.TotalCreated,
                    totalUsed = stats.TotalUsed,
                    totalActive = stats.TotalActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching overall discount stats");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching statistics" });
            }
        }

        /// <summary>
        /// Gets all discount usage statistics
        /// </summary>
        [HttpGet("analytics/usage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllDiscountUsageStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _discountAnalyticsService.GetAllDiscountUsageStatsAsync(
                    startDate, endDate, cancellationToken);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount usage stats");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching usage statistics" });
            }
        }

        /// <summary>
        /// Gets all discount performance metrics
        /// </summary>
        [HttpGet("analytics/performance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllDiscountPerformance(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var performance = await _discountAnalyticsService.GetAllDiscountPerformanceAsync(
                    startDate, endDate, cancellationToken);

                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount performance");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching performance metrics" });
            }
        }

        /// <summary>
        /// Gets top performing discounts
        /// </summary>
        [HttpGet("analytics/top-performing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetTopPerformingDiscounts(
            [FromQuery] int topCount = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var topDiscounts = await _discountAnalyticsService.GetTopPerformingDiscountsAsync(
                    topCount, startDate, endDate, cancellationToken);

                return Ok(topDiscounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top performing discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching top performing discounts" });
            }
        }

        /// <summary>
        /// Gets revenue impact analysis
        /// </summary>
        [HttpGet("analytics/revenue-impact")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDiscountRevenueImpact(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var impact = await _discountAnalyticsService.GetDiscountRevenueImpactAsync(
                    startDate, endDate, cancellationToken);

                return Ok(impact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount revenue impact");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching revenue impact" });
            }
        }

        /// <summary>
        /// Updates any discount (admin override)
        /// </summary>
        [HttpPut("{discountId:guid}")]
        [ProducesResponseType(typeof(DiscountResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<DiscountResponse>> UpdateDiscount(
            Guid discountId,
            [FromBody] DiscountUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.DiscountId != discountId)
                {
                    return BadRequest(new { message = "Discount ID mismatch" });
                }

                var discount = await _discountManagementService.UpdateDiscountAsync(request, cancellationToken);

                return Ok(discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the discount" });
            }
        }

        /// <summary>
        /// Deletes any discount (admin override)
        /// </summary>
        [HttpDelete("{discountId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteDiscount(
            Guid discountId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _discountManagementService.DeleteDiscountAsync(discountId, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Discount not found" });
                }

                return Ok(new { message = "Discount deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discount");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the discount" });
            }
        }
    }
}
