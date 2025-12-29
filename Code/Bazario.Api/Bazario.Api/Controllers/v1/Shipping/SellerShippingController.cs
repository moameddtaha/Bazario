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
    /// Seller API for managing store shipping configurations
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seller/shipping")]
    [Tags("Seller - Shipping")]
    [Authorize(Roles = "Seller")]
    public class SellerShippingController : ControllerBase
    {
        private readonly IStoreShippingConfigurationService _shippingConfigService;
        private readonly ILogger<SellerShippingController> _logger;

        public SellerShippingController(
            IStoreShippingConfigurationService shippingConfigService,
            ILogger<SellerShippingController> logger)
        {
            _shippingConfigService = shippingConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the shipping configuration for seller's store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Shipping configuration</returns>
        /// <response code="200">Returns shipping configuration</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not the store owner</response>
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
                _logger.LogInformation("Seller fetching shipping configuration for store {StoreId}", storeId);

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
        /// Creates a new shipping configuration for seller's store
        /// </summary>
        /// <param name="request">Shipping configuration details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created shipping configuration</returns>
        /// <response code="201">Shipping configuration created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not the store owner</response>
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

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Seller {UserId} creating shipping configuration for store {StoreId}",
                    userId, request.StoreId);

                var configuration = await _shippingConfigService.CreateConfigurationAsync(
                    request, userId, cancellationToken);

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
        /// Updates an existing shipping configuration for seller's store
        /// </summary>
        /// <param name="request">Updated shipping configuration details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated shipping configuration</returns>
        /// <response code="200">Shipping configuration updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        /// <response code="403">Forbidden - user is not the store owner</response>
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

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Seller {UserId} updating shipping configuration for store {StoreId}",
                    userId, request.StoreId);

                var configuration = await _shippingConfigService.UpdateConfigurationAsync(
                    request, userId, cancellationToken);

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
        /// Checks if same-day delivery is available for seller's store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Availability status</returns>
        /// <response code="200">Returns availability status</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        [HttpGet("stores/{storeId:guid}/same-day-availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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

                _logger.LogInformation("Seller checking same-day delivery availability for store {StoreId} to city {City}",
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
        /// Calculates delivery fee for seller's store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery fee</returns>
        /// <response code="200">Returns delivery fee</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized - user is not authenticated</response>
        [HttpGet("stores/{storeId:guid}/delivery-fee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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

                _logger.LogInformation("Seller calculating delivery fee for store {StoreId} to city {City}",
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
