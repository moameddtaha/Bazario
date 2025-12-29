using Asp.Versioning;
using Bazario.Core.DTO.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Shipping
{
    /// <summary>
    /// Admin API for managing all store shipping configurations
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/shipping")]
    [Tags("Admin - Shipping")]
    [Authorize(Roles = "Admin")]
    public class AdminShippingController : ControllerBase
    {
        private readonly IStoreShippingConfigurationService _shippingConfigService;
        private readonly ILogger<AdminShippingController> _logger;

        public AdminShippingController(
            IStoreShippingConfigurationService shippingConfigService,
            ILogger<AdminShippingController> logger)
        {
            _shippingConfigService = shippingConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the shipping configuration for any store (Admin only)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Shipping configuration</returns>
        /// <response code="200">Returns shipping configuration</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        [HttpGet("stores/{storeId:guid}/configuration")]
        [ProducesResponseType(typeof(StoreShippingConfigurationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<StoreShippingConfigurationResponse>> GetConfiguration(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching shipping configuration for store {StoreId}", storeId);

                var configuration = await _shippingConfigService.GetConfigurationAsync(storeId, cancellationToken);

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shipping configuration for store {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching shipping configuration" });
            }
        }

        /// <summary>
        /// Creates a new shipping configuration for any store (Admin only)
        /// </summary>
        /// <param name="request">Shipping configuration details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created shipping configuration</returns>
        /// <response code="201">Shipping configuration created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        /// <response code="409">Conflict - configuration already exists</response>
        [HttpPost("configuration")]
        [ProducesResponseType(typeof(StoreShippingConfigurationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<StoreShippingConfigurationResponse>> CreateConfiguration(
            [FromBody] StoreShippingConfigurationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Admin {AdminId} creating shipping configuration for store {StoreId}",
                    adminId, request.StoreId);

                var configuration = await _shippingConfigService.CreateConfigurationAsync(
                    request, adminId, cancellationToken);

                return CreatedAtAction(
                    nameof(GetConfiguration),
                    new { storeId = configuration.StoreId },
                    configuration);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating shipping configuration");
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while creating shipping configuration");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping configuration");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating shipping configuration" });
            }
        }

        /// <summary>
        /// Updates an existing shipping configuration for any store (Admin only)
        /// </summary>
        /// <param name="request">Updated shipping configuration details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated shipping configuration</returns>
        /// <response code="200">Shipping configuration updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        /// <response code="404">Not found - configuration does not exist</response>
        [HttpPut("configuration")]
        [ProducesResponseType(typeof(StoreShippingConfigurationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreShippingConfigurationResponse>> UpdateConfiguration(
            [FromBody] StoreShippingConfigurationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Admin {AdminId} updating shipping configuration for store {StoreId}",
                    adminId, request.StoreId);

                var configuration = await _shippingConfigService.UpdateConfigurationAsync(
                    request, adminId, cancellationToken);

                return Ok(configuration);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating shipping configuration");
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while updating shipping configuration");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping configuration");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating shipping configuration" });
            }
        }

        /// <summary>
        /// Permanently deletes a shipping configuration (Admin only)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="reason">Reason for deletion (required for audit trail)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deletion status</returns>
        /// <response code="204">Shipping configuration deleted successfully</response>
        /// <response code="400">Invalid request - reason is required</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        /// <response code="404">Not found - configuration does not exist</response>
        [HttpDelete("stores/{storeId:guid}/configuration")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteConfiguration(
            Guid storeId,
            [FromQuery] string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                {
                    return BadRequest(new { message = "Deletion reason is required and must be at least 10 characters" });
                }

                var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogWarning("Admin {AdminId} deleting shipping configuration for store {StoreId}. Reason: {Reason}",
                    adminId, storeId, reason);

                var deleted = await _shippingConfigService.DeleteConfigurationAsync(
                    storeId, adminId, reason, cancellationToken);

                if (!deleted)
                {
                    return NotFound(new { message = "Shipping configuration not found" });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while deleting shipping configuration");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while deleting shipping configuration");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shipping configuration for store {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting shipping configuration" });
            }
        }

        /// <summary>
        /// Checks if same-day delivery is available for any store to a specific city (Admin only)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Availability status</returns>
        /// <response code="200">Returns availability status</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        [HttpGet("stores/{storeId:guid}/same-day-availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CheckSameDayDeliveryAvailability(
            Guid storeId,
            [FromQuery] string city,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "City is required" });
                }

                _logger.LogInformation("Admin checking same-day delivery availability for store {StoreId} to city {City}",
                    storeId, city);

                var isAvailable = await _shippingConfigService.IsSameDayDeliveryAvailableAsync(
                    storeId, city, cancellationToken);

                return Ok(new
                {
                    storeId,
                    city,
                    sameDayDeliveryAvailable = isAvailable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking same-day delivery availability");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while checking delivery availability" });
            }
        }

        /// <summary>
        /// Calculates delivery fee for any store to a specific city (Admin only)
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery fee</returns>
        /// <response code="200">Returns delivery fee</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not an admin</response>
        [HttpGet("stores/{storeId:guid}/delivery-fee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetDeliveryFee(
            Guid storeId,
            [FromQuery] string city,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "City is required" });
                }

                _logger.LogInformation("Admin calculating delivery fee for store {StoreId} to city {City}",
                    storeId, city);

                var fee = await _shippingConfigService.GetDeliveryFeeAsync(
                    storeId, city, cancellationToken);

                return Ok(new
                {
                    storeId,
                    city,
                    deliveryFee = fee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery fee");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while calculating delivery fee" });
            }
        }
    }
}
