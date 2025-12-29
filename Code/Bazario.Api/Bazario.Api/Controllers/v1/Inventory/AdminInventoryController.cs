using Asp.Versioning;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Inventory
{
    /// <summary>
    /// Admin API for inventory management and analytics
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/inventory")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Inventory")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryManagementService _inventoryManagementService;
        private readonly IInventoryQueryService _inventoryQueryService;
        private readonly IInventoryAnalyticsService _inventoryAnalyticsService;
        private readonly IInventoryAlertService _inventoryAlertService;
        private readonly ILogger<AdminInventoryController> _logger;

        public AdminInventoryController(
            IInventoryManagementService inventoryManagementService,
            IInventoryQueryService inventoryQueryService,
            IInventoryAnalyticsService inventoryAnalyticsService,
            IInventoryAlertService inventoryAlertService,
            ILogger<AdminInventoryController> logger)
        {
            _inventoryManagementService = inventoryManagementService;
            _inventoryQueryService = inventoryQueryService;
            _inventoryAnalyticsService = inventoryAnalyticsService;
            _inventoryAlertService = inventoryAlertService;
            _logger = logger;
        }

        /// <summary>
        /// Updates stock for a product
        /// </summary>
        [HttpPut("products/{productId:guid}/stock")]
        [ProducesResponseType(typeof(InventoryUpdateResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<InventoryUpdateResult>> UpdateStock(
            Guid productId,
            [FromQuery] int newQuantity,
            [FromQuery] StockUpdateType updateType,
            [FromQuery] string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _inventoryManagementService.UpdateStockAsync(
                    productId, newQuantity, updateType, reason, userId, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating stock" });
            }
        }

        /// <summary>
        /// Processes pending inventory alerts
        /// </summary>
        [HttpPost("alerts/process-pending")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> ProcessPendingAlerts(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await _inventoryAlertService.ProcessPendingAlertsAsync(cancellationToken);
                return Ok(new { message = $"Processed {count} alerts successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending alerts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing alerts" });
            }
        }

        /// <summary>
        /// Cleans up expired reservations
        /// </summary>
        [HttpPost("reservations/cleanup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CleanupExpiredReservations(
            [FromQuery] int expirationMinutes = 30,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await _inventoryManagementService.CleanupExpiredReservationsAsync(
                    expirationMinutes, cancellationToken);

                return Ok(new { message = $"Cleaned up {count} expired reservations" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired reservations");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred during cleanup" });
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
