using Asp.Versioning;
using Bazario.Core.DTO.Store;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Store
{
    /// <summary>
    /// Seller API for managing their own stores
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seller/stores")]
    [Authorize(Roles = "Seller")]
    [Tags("Seller - Stores")]
    public class SellerStoreController : ControllerBase
    {
        private readonly IStoreManagementService _storeManagementService;
        private readonly IStoreQueryService _storeQueryService;
        private readonly IStoreAnalyticsService _storeAnalyticsService;
        private readonly IStoreAuthorizationService _storeAuthorizationService;
        private readonly ILogger<SellerStoreController> _logger;

        public SellerStoreController(
            IStoreManagementService storeManagementService,
            IStoreQueryService storeQueryService,
            IStoreAnalyticsService storeAnalyticsService,
            IStoreAuthorizationService storeAuthorizationService,
            ILogger<SellerStoreController> logger)
        {
            _storeManagementService = storeManagementService;
            _storeQueryService = storeQueryService;
            _storeAnalyticsService = storeAnalyticsService;
            _storeAuthorizationService = storeAuthorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all stores owned by the current seller
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of seller's stores</returns>
        /// <response code="200">Returns the seller's stores</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("my-stores")]
        [ProducesResponseType(typeof(List<StoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<StoreResponse>>> GetMyStores(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();
                _logger.LogInformation("Fetching stores for seller: {SellerId}", sellerId);

                var stores = await _storeQueryService.GetStoresBySellerIdAsync(sellerId, cancellationToken);

                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching seller stores");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching your stores" });
            }
        }

        /// <summary>
        /// Gets a specific store by ID (only if owned by seller)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store details</returns>
        /// <response code="200">Returns the store</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to view this store</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> GetStoreById(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();
                _logger.LogInformation("Seller {SellerId} fetching store: {StoreId}", sellerId, storeId);

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to access unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view this store" });
                }

                var store = await _storeQueryService.GetStoreByIdAsync(storeId, cancellationToken);

                if (store == null)
                {
                    _logger.LogWarning("Store not found: {StoreId}", storeId);
                    return NotFound(new { message = "Store not found" });
                }

                return Ok(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the store" });
            }
        }

        /// <summary>
        /// Creates a new store for the current seller
        /// </summary>
        /// <param name="request">Store creation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created store</returns>
        /// <response code="201">Store created successfully</response>
        /// <response code="400">Invalid request or validation failed</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<StoreResponse>> CreateStore(
            [FromBody] StoreAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                if (request.SellerId != sellerId)
                {
                    _logger.LogWarning("Seller ID mismatch. Token: {TokenId}, Request: {RequestId}",
                        sellerId, request.SellerId);
                    return BadRequest(new { message = "You can only create stores for yourself" });
                }

                _logger.LogInformation("Creating store for seller: {SellerId}", sellerId);

                var store = await _storeManagementService.CreateStoreAsync(request, cancellationToken);

                _logger.LogInformation("Store created successfully: {StoreId}", store.StoreId);

                return CreatedAtAction(
                    nameof(PublicStoreController.GetStoreById),
                    "PublicStore",
                    new { storeId = store.StoreId, version = "1.0" },
                    store);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid store creation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store creation failed due to business rule");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the store" });
            }
        }

        /// <summary>
        /// Updates an existing store
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="request">Store update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store</returns>
        /// <response code="200">Store updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to update this store</response>
        /// <response code="404">Store not found</response>
        /// <response code="409">Optimistic concurrency conflict</response>
        [HttpPut("{storeId:guid}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<StoreResponse>> UpdateStore(
            Guid storeId,
            [FromBody] StoreUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.StoreId != storeId)
                {
                    return BadRequest(new { message = "Store ID in URL and body must match" });
                }

                var sellerId = GetCurrentUserId();

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to update unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to update this store" });
                }

                _logger.LogInformation("Updating store: {StoreId} by seller: {SellerId}", storeId, sellerId);

                var store = await _storeManagementService.UpdateStoreAsync(request, cancellationToken);

                _logger.LogInformation("Store updated successfully: {StoreId}", storeId);

                return Ok(store);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid store update request");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store update failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("concurrency"))
            {
                _logger.LogWarning(ex, "Concurrency conflict updating store: {StoreId}", storeId);
                return Conflict(new { message = "The store was modified by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the store" });
            }
        }

        /// <summary>
        /// Soft deletes a store
        /// </summary>
        /// <param name="storeId">The store ID to delete</param>
        /// <param name="reason">Optional reason for deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Store deleted successfully</response>
        /// <response code="400">Store cannot be deleted</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to delete this store</response>
        /// <response code="404">Store not found</response>
        [HttpDelete("{storeId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteStore(
            Guid storeId,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to delete unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to delete this store" });
                }

                _logger.LogInformation("Deleting store: {StoreId} by seller: {SellerId}", storeId, sellerId);

                var result = await _storeManagementService.DeleteStoreAsync(
                    storeId, sellerId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Store not found or already deleted" });
                }

                _logger.LogInformation("Store deleted successfully: {StoreId}", storeId);

                return Ok(new { message = "Store deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store deletion not allowed: {StoreId}", storeId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the store" });
            }
        }

        /// <summary>
        /// Restores a soft-deleted store
        /// </summary>
        /// <param name="storeId">The store ID to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restored store</returns>
        /// <response code="200">Store restored successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to restore this store</response>
        /// <response code="404">Store not found</response>
        [HttpPost("{storeId:guid}/restore")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> RestoreStore(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                _logger.LogInformation("Restoring store: {StoreId} by seller: {SellerId}", storeId, sellerId);

                var store = await _storeManagementService.RestoreStoreAsync(storeId, sellerId, cancellationToken);

                // Verify seller can manage the restored store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    // If not authorized, delete it again
                    await _storeManagementService.DeleteStoreAsync(storeId, sellerId, "Unauthorized restore attempt", cancellationToken);

                    _logger.LogWarning("Seller {SellerId} attempted to restore unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to restore this store" });
                }

                _logger.LogInformation("Store restored successfully: {StoreId}", storeId);

                return Ok(store);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store restore failed: {StoreId}", storeId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while restoring the store" });
            }
        }

        /// <summary>
        /// Gets analytics for the seller's store
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="startDate">Optional start date for analytics</param>
        /// <param name="endDate">Optional end date for analytics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store analytics data</returns>
        /// <response code="200">Returns the store analytics</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to view this store's analytics</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}/analytics")]
        [ProducesResponseType(typeof(StoreAnalytics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreAnalytics>> GetStoreAnalytics(
            Guid storeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to access analytics for unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view this store's analytics" });
                }

                _logger.LogInformation("Fetching analytics for store: {StoreId}", storeId);

                var dateRange = (startDate.HasValue && endDate.HasValue)
                    ? new DateRange { StartDate = startDate.Value, EndDate = endDate.Value }
                    : null;

                var analytics = await _storeAnalyticsService.GetStoreAnalyticsAsync(storeId, dateRange, cancellationToken);

                return Ok(analytics);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = "Store not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store analytics: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching store analytics" });
            }
        }

        /// <summary>
        /// Gets performance summary for the seller's store
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store performance data</returns>
        /// <response code="200">Returns the store performance</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to view this store's performance</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}/performance")]
        [ProducesResponseType(typeof(StorePerformance), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StorePerformance>> GetStorePerformance(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, storeId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to access performance for unauthorized store {StoreId}",
                        sellerId, storeId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view this store's performance" });
                }

                _logger.LogInformation("Fetching performance for store: {StoreId}", storeId);

                var performance = await _storeAnalyticsService.GetStorePerformanceAsync(storeId, cancellationToken);

                return Ok(performance);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = "Store not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store performance: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching store performance" });
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
