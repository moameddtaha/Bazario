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
    /// Admin API for store management and oversight
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/stores")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Stores")]
    public class AdminStoreController : ControllerBase
    {
        private readonly IStoreManagementService _storeManagementService;
        private readonly IStoreQueryService _storeQueryService;
        private readonly IStoreAnalyticsService _storeAnalyticsService;
        private readonly ILogger<AdminStoreController> _logger;

        public AdminStoreController(
            IStoreManagementService storeManagementService,
            IStoreQueryService storeQueryService,
            IStoreAnalyticsService storeAnalyticsService,
            ILogger<AdminStoreController> logger)
        {
            _storeManagementService = storeManagementService;
            _storeQueryService = storeQueryService;
            _storeAnalyticsService = storeAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Searches all stores including deleted ones (admin view)
        /// </summary>
        /// <param name="searchTerm">Search term for store name</param>
        /// <param name="category">Filter by category</param>
        /// <param name="sellerId">Filter by seller ID</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="includeDeleted">Include soft-deleted stores</param>
        /// <param name="onlyDeleted">Show only deleted stores</param>
        /// <param name="sortBy">Sort field (Name, CreatedAt, Rating, Revenue)</param>
        /// <param name="sortDescending">Sort descending (default: false)</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of stores</returns>
        /// <response code="200">Returns matching stores</response>
        /// <response code="400">Invalid search parameters</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResponse<StoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<StoreResponse>>> SearchStores(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null,
            [FromQuery] Guid? sellerId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] bool onlyDeleted = false,
            [FromQuery] string? sortBy = "Name",
            [FromQuery] bool sortDescending = false,
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

                _logger.LogInformation(
                    "Admin searching stores with term: {SearchTerm}, Category: {Category}, IncludeDeleted: {IncludeDeleted}",
                    searchTerm, category, includeDeleted);

                var searchCriteria = new StoreSearchCriteria
                {
                    SearchTerm = searchTerm,
                    Category = category,
                    SellerId = sellerId,
                    IsActive = isActive,
                    IncludeDeleted = includeDeleted,
                    OnlyDeleted = onlyDeleted,
                    SortBy = sortBy,
                    SortDescending = sortDescending,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var stores = await _storeQueryService.SearchStoresAsync(searchCriteria, cancellationToken);

                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stores");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while searching stores" });
            }
        }

        /// <summary>
        /// Gets a specific store by ID (admin can view any store)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store details</returns>
        /// <response code="200">Returns the store</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> GetStoreById(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching store: {StoreId}", storeId);

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
        /// Gets all stores for a specific seller
        /// </summary>
        /// <param name="sellerId">The seller ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of seller's stores</returns>
        /// <response code="200">Returns the seller's stores</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("seller/{sellerId:guid}")]
        [ProducesResponseType(typeof(List<StoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<StoreResponse>>> GetStoresBySeller(
            Guid sellerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching stores for seller: {SellerId}", sellerId);

                var stores = await _storeQueryService.GetStoresBySellerIdAsync(sellerId, cancellationToken);

                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores for seller: {SellerId}", sellerId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching stores" });
            }
        }

        /// <summary>
        /// Updates any store (admin override)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="request">Store update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store</returns>
        /// <response code="200">Store updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        /// <response code="409">Optimistic concurrency conflict</response>
        [HttpPut("{storeId:guid}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} updating store: {StoreId}", adminId, storeId);

                var store = await _storeManagementService.UpdateStoreAsync(request, cancellationToken);

                _logger.LogInformation("Store updated successfully by admin: {StoreId}", storeId);

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
        /// Updates store status (active/inactive)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="isActive">New active status</param>
        /// <param name="reason">Reason for status change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated store</returns>
        /// <response code="200">Store status updated successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpPut("{storeId:guid}/status")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> UpdateStoreStatus(
            Guid storeId,
            [FromQuery] bool isActive,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} updating status for store: {StoreId} to {IsActive}",
                    adminId, storeId, isActive);

                var store = await _storeManagementService.UpdateStoreStatusAsync(
                    storeId, adminId, isActive, reason, cancellationToken);

                _logger.LogInformation("Store status updated successfully: {StoreId}", storeId);

                return Ok(store);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = "Store not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized status update attempt");
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store status: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating store status" });
            }
        }

        /// <summary>
        /// Soft deletes a store
        /// </summary>
        /// <param name="storeId">The store ID to delete</param>
        /// <param name="reason">Reason for deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Store deleted successfully</response>
        /// <response code="400">Store cannot be deleted</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpDelete("{storeId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SoftDeleteStore(
            Guid storeId,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} soft deleting store: {StoreId}", adminId, storeId);

                var result = await _storeManagementService.DeleteStoreAsync(
                    storeId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Store not found or already deleted" });
                }

                _logger.LogInformation("Store soft deleted by admin: {StoreId}", storeId);

                return Ok(new { message = "Store soft deleted successfully" });
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
                _logger.LogError(ex, "Error soft deleting store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the store" });
            }
        }

        /// <summary>
        /// Permanently deletes a store (hard delete - requires admin)
        /// </summary>
        /// <param name="storeId">The store ID to permanently delete</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Store permanently deleted successfully</response>
        /// <response code="400">Reason is required</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpDelete("{storeId:guid}/hard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> HardDeleteStore(
            Guid storeId,
            [FromBody] string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(new { message = "Reason is required for hard deletion" });
                }

                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} hard deleting store: {StoreId} with reason: {Reason}",
                    adminId, storeId, reason);

                var result = await _storeManagementService.HardDeleteStoreAsync(
                    storeId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Store not found" });
                }

                _logger.LogInformation("Store hard deleted by admin: {StoreId}", storeId);

                return Ok(new { message = "Store permanently deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Store not found: {StoreId}", storeId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized hard delete attempt");
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting store: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while permanently deleting the store" });
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
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpPost("{storeId:guid}/restore")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> RestoreStore(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} restoring store: {StoreId}", adminId, storeId);

                var store = await _storeManagementService.RestoreStoreAsync(storeId, adminId, cancellationToken);

                _logger.LogInformation("Store restored by admin: {StoreId}", storeId);

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
        /// Gets analytics for any store (admin view)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="startDate">Optional start date for analytics</param>
        /// <param name="endDate">Optional end date for analytics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store analytics data</returns>
        /// <response code="200">Returns the store analytics</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}/analytics")]
        [ProducesResponseType(typeof(StoreAnalytics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreAnalytics>> GetStoreAnalytics(
            Guid storeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching analytics for store: {StoreId}", storeId);

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
        /// Gets performance summary for any store (admin view)
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store performance data</returns>
        /// <response code="200">Returns the store performance</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}/performance")]
        [ProducesResponseType(typeof(StorePerformance), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StorePerformance>> GetStorePerformance(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching performance for store: {StoreId}", storeId);

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

        /// <summary>
        /// Gets top performing stores (admin view with all filters)
        /// </summary>
        /// <param name="performanceCriteria">Performance criteria (Revenue, OrderCount, Rating)</param>
        /// <param name="category">Optional category filter</param>
        /// <param name="sellerId">Optional seller filter</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 50)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Top performing stores</returns>
        /// <response code="200">Returns top performing stores</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("top-performing")]
        [ProducesResponseType(typeof(PagedResponse<StorePerformance>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<StorePerformance>>> GetTopPerformingStores(
            [FromQuery] string performanceCriteria = "Revenue",
            [FromQuery] string? category = null,
            [FromQuery] Guid? sellerId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
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

                if (!Enum.TryParse<Core.Enums.Store.PerformanceCriteria>(performanceCriteria, out var criteria))
                {
                    return BadRequest(new { message = "Invalid performance criteria. Valid values: Revenue, OrderCount, Rating" });
                }

                _logger.LogInformation("Admin fetching top performing stores by: {Criteria}", performanceCriteria);

                var searchCriteria = new StoreSearchCriteria
                {
                    Category = category,
                    SellerId = sellerId,
                    IsActive = true,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var stores = await _storeAnalyticsService.GetTopPerformingStoresAsync(
                    criteria, searchCriteria, cancellationToken);

                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top performing stores");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching top performing stores" });
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
