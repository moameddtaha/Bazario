using Asp.Versioning;
using Bazario.Core.Enums.Order;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Shipping
{
    /// <summary>
    /// Public API for checking delivery availability and fees
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/shipping")]
    [Tags("Public - Shipping")]
    public class PublicShippingController : ControllerBase
    {
        private readonly IStoreShippingConfigurationService _shippingConfigService;
        private readonly IShippingZoneService _shippingZoneService;
        private readonly ILogger<PublicShippingController> _logger;

        public PublicShippingController(
            IStoreShippingConfigurationService shippingConfigService,
            IShippingZoneService shippingZoneService,
            ILogger<PublicShippingController> logger)
        {
            _shippingConfigService = shippingConfigService;
            _shippingZoneService = shippingZoneService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if same-day delivery is available for a store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Availability status</returns>
        /// <response code="200">Returns availability status</response>
        /// <response code="400">Invalid request</response>
        [HttpGet("stores/{storeId:guid}/same-day-availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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

                _logger.LogInformation("Checking same-day delivery availability for store {StoreId} to city {City}",
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
        /// Calculates delivery fee for a store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery fee</returns>
        /// <response code="200">Returns delivery fee</response>
        /// <response code="400">Invalid request</response>
        [HttpGet("stores/{storeId:guid}/delivery-fee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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

                _logger.LogInformation("Calculating delivery fee for store {StoreId} to city {City}",
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

        /// <summary>
        /// Gets available delivery options for a store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="country">Country code (default: EG)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available delivery options</returns>
        /// <response code="200">Returns available delivery options</response>
        /// <response code="400">Invalid request</response>
        [HttpGet("stores/{storeId:guid}/delivery-options")]
        [ProducesResponseType(typeof(List<ShippingZone>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<ShippingZone>>> GetDeliveryOptions(
            Guid storeId,
            [FromQuery] string city,
            [FromQuery] string country = "EG",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "City is required" });
                }

                _logger.LogInformation("Fetching delivery options for store {StoreId} to city {City}, country {Country}",
                    storeId, city, country);

                var options = await _shippingZoneService.GetAvailableDeliveryOptionsAsync(
                    storeId, city, country, cancellationToken);

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery options");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching delivery options" });
            }
        }

        /// <summary>
        /// Determines shipping zone for a store to a specific city
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="city">City name</param>
        /// <param name="country">Country code (default: EG)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Shipping zone</returns>
        /// <response code="200">Returns shipping zone</response>
        /// <response code="400">Invalid request</response>
        [HttpGet("stores/{storeId:guid}/shipping-zone")]
        [ProducesResponseType(typeof(ShippingZone), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShippingZone>> GetShippingZone(
            Guid storeId,
            [FromQuery] string city,
            [FromQuery] string country = "EG",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "City is required" });
                }

                _logger.LogInformation("Determining shipping zone for store {StoreId} to city {City}, country {Country}",
                    storeId, city, country);

                var zone = await _shippingZoneService.DetermineStoreShippingZoneAsync(
                    storeId, city, country, cancellationToken);

                return Ok(zone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining shipping zone");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while determining shipping zone" });
            }
        }
    }
}
