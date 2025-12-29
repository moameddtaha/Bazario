using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Discount
{
    /// <summary>
    /// Public API for validating discount codes
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/discounts")]
    [Tags("Public - Discounts")]
    public class PublicDiscountController : ControllerBase
    {
        private readonly IDiscountValidationService _discountValidationService;
        private readonly IDiscountManagementService _discountManagementService;
        private readonly ILogger<PublicDiscountController> _logger;

        public PublicDiscountController(
            IDiscountValidationService discountValidationService,
            IDiscountManagementService discountManagementService,
            ILogger<PublicDiscountController> logger)
        {
            _discountValidationService = discountValidationService;
            _discountManagementService = discountManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Validates a discount code for an order
        /// </summary>
        /// <param name="code">Discount code to validate</param>
        /// <param name="orderSubtotal">Order subtotal amount</param>
        /// <param name="storeIds">Store IDs in the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with discount details if valid</returns>
        /// <response code="200">Returns validation result</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ValidateDiscountCode(
            [FromQuery] string code,
            [FromQuery] decimal orderSubtotal,
            [FromBody] List<Guid> storeIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return BadRequest(new { message = "Discount code is required" });
                }

                if (orderSubtotal <= 0)
                {
                    return BadRequest(new { message = "Order subtotal must be greater than 0" });
                }

                _logger.LogInformation("Validating discount code: {Code} for order subtotal: {Subtotal}",
                    code, orderSubtotal);

                var (isValid, discount, errorMessage) = await _discountValidationService.ValidateDiscountCodeAsync(
                    code, orderSubtotal, storeIds, cancellationToken);

                if (!isValid)
                {
                    return Ok(new { isValid = false, message = errorMessage });
                }

                return Ok(new { isValid = true, discount, message = "Discount code is valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating discount code: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while validating the discount code" });
            }
        }

        /// <summary>
        /// Gets all active global discounts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active global discounts</returns>
        /// <response code="200">Returns active discounts</response>
        [HttpGet("global/active")]
        [ProducesResponseType(typeof(List<DiscountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DiscountResponse>>> GetActiveGlobalDiscounts(
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching active global discounts");

                var discounts = await _discountManagementService.GetGlobalDiscountsAsync(cancellationToken);

                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active global discounts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching discounts" });
            }
        }
    }
}
