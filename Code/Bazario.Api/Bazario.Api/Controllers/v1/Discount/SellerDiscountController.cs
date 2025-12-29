using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Discount
{
    /// <summary>
    /// Seller API for managing store-specific discounts
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seller/discounts")]
    [Authorize(Roles = "Seller")]
    [Tags("Seller - Discounts")]
    public class SellerDiscountController : ControllerBase
    {
        private readonly IDiscountManagementService _discountManagementService;
        private readonly IDiscountAnalyticsService _discountAnalyticsService;
        private readonly IStoreAuthorizationService _storeAuthorizationService;
        private readonly ILogger<SellerDiscountController> _logger;

        public SellerDiscountController(
            IDiscountManagementService discountManagementService,
            IDiscountAnalyticsService discountAnalyticsService,
            IStoreAuthorizationService storeAuthorizationService,
            ILogger<SellerDiscountController> logger)
        {
            _discountManagementService = discountManagementService;
            _discountAnalyticsService = discountAnalyticsService;
            _storeAuthorizationService = storeAuthorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all discounts for the seller's store
        /// </summary>
        [HttpGet("my-discounts")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetMyStoreDiscounts(
            [FromQuery] Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view discounts for this store" });
                }

                var discounts = await _discountManagementService.GetStoreDiscountsAsync(storeId, cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching discounts" });
            }
        }

        /// <summary>
        /// Creates a new discount for the seller's store
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DiscountResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DiscountResponse>> CreateDiscount(
            [FromBody] DiscountAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                if (request.ApplicableStoreId.HasValue)
                {
                    var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                        sellerId, request.ApplicableStoreId.Value, cancellationToken);

                    if (!canManage)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { message = "You are not authorized to create discounts for this store" });
                    }
                }

                request.CreatedBy = sellerId;

                var discount = await _discountManagementService.CreateDiscountAsync(request, cancellationToken);

                return CreatedAtAction(
                    nameof(GetDiscountById),
                    new { discountId = discount.DiscountId, version = "1.0" },
                    discount);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid discount creation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discount");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the discount" });
            }
        }

        /// <summary>
        /// Gets a discount by ID
        /// </summary>
        [HttpGet("{discountId:guid}")]
        [ProducesResponseType(typeof(DiscountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DiscountResponse>> GetDiscountById(
            Guid discountId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();
                var discount = await _discountManagementService.GetDiscountByIdAsync(discountId, cancellationToken);

                if (discount == null)
                {
                    return NotFound(new { message = "Discount not found" });
                }

                if (discount.ApplicableStoreId.HasValue)
                {
                    var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                        sellerId, discount.ApplicableStoreId.Value, cancellationToken);

                    if (!canManage)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { message = "You are not authorized to view this discount" });
                    }
                }

                return Ok(discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the discount" });
            }
        }

        /// <summary>
        /// Updates a discount
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

                var sellerId = GetCurrentUserId();
                var existingDiscount = await _discountManagementService.GetDiscountByIdAsync(discountId, cancellationToken);

                if (existingDiscount?.ApplicableStoreId.HasValue == true)
                {
                    var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                        sellerId, existingDiscount.ApplicableStoreId.Value, cancellationToken);

                    if (!canManage)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { message = "You are not authorized to update this discount" });
                    }
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
        /// Deletes a discount
        /// </summary>
        [HttpDelete("{discountId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteDiscount(
            Guid discountId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();
                var discount = await _discountManagementService.GetDiscountByIdAsync(discountId, cancellationToken);

                if (discount?.ApplicableStoreId.HasValue == true)
                {
                    var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                        sellerId, discount.ApplicableStoreId.Value, cancellationToken);

                    if (!canManage)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { message = "You are not authorized to delete this discount" });
                    }
                }

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

        /// <summary>
        /// Gets discount usage statistics for a store
        /// </summary>
        [HttpGet("analytics/store/{storeId:guid}/usage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetStoreDiscountUsageStats(
            Guid storeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view analytics for this store" });
                }

                var stats = await _discountAnalyticsService.GetStoreDiscountUsageStatsAsync(
                    storeId, startDate, endDate, cancellationToken);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount usage stats");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching usage statistics" });
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
