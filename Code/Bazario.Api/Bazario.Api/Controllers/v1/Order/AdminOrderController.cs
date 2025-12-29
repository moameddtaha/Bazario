using Asp.Versioning;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Order
{
    /// <summary>
    /// Admin API for order management and oversight
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/orders")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Orders")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderManagementService _orderManagementService;
        private readonly IOrderQueryService _orderQueryService;
        private readonly IOrderAnalyticsService _orderAnalyticsService;
        private readonly ILogger<AdminOrderController> _logger;

        public AdminOrderController(
            IOrderManagementService orderManagementService,
            IOrderQueryService orderQueryService,
            IOrderAnalyticsService orderAnalyticsService,
            ILogger<AdminOrderController> logger)
        {
            _orderManagementService = orderManagementService;
            _orderQueryService = orderQueryService;
            _orderAnalyticsService = orderAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all orders by status with pagination
        /// </summary>
        /// <param name="status">Order status to filter by</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of orders</returns>
        /// <response code="200">Returns the paginated orders</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOrdersByStatus(
            OrderStatus status,
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

                _logger.LogInformation("Admin fetching orders by status: {Status}, Page: {PageNumber}, Size: {PageSize}",
                    status, pageNumber, pageSize);

                var orders = await _orderQueryService.GetOrdersByStatusAsync(status, pageNumber, pageSize, cancellationToken);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders by status: {Status}", status);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching orders" });
            }
        }

        /// <summary>
        /// Searches orders with advanced filtering
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        /// <response code="200">Returns the search results</response>
        /// <response code="400">Invalid search criteria</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<OrderResponse>>> SearchOrders(
            [FromBody] OrderSearchCriteria searchCriteria,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (searchCriteria == null)
                {
                    return BadRequest(new { message = "Search criteria cannot be null" });
                }

                _logger.LogInformation("Admin searching orders with criteria");

                var results = await _orderQueryService.SearchOrdersAsync(searchCriteria, cancellationToken);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching orders");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while searching orders" });
            }
        }

        /// <summary>
        /// Gets a specific order by ID (admin view)
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order details</returns>
        /// <response code="200">Returns the order</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> GetOrderById(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching order: {OrderId}", orderId);

                var order = await _orderQueryService.GetOrderByIdAsync(orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the order" });
            }
        }

        /// <summary>
        /// Gets all orders for a specific customer (admin view)
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer's orders</returns>
        /// <response code="200">Returns the customer's orders</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("customer/{customerId:guid}")]
        [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<OrderResponse>>> GetCustomerOrders(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching orders for customer: {CustomerId}", customerId);

                var orders = await _orderQueryService.GetOrdersByCustomerIdAsync(customerId, cancellationToken);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer orders: {CustomerId}", customerId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching customer orders" });
            }
        }

        /// <summary>
        /// Updates order status
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="newStatus">New status to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated order</returns>
        /// <response code="200">Order status updated successfully</response>
        /// <response code="400">Invalid status transition</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpPut("{orderId:guid}/status")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(
            Guid orderId,
            [FromQuery] OrderStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} updating order {OrderId} status to {NewStatus}",
                    adminId, orderId, newStatus);

                var order = await _orderManagementService.UpdateOrderStatusAsync(orderId, newStatus, cancellationToken);

                _logger.LogInformation("Order status updated successfully: {OrderId}", orderId);

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid status transition for order: {OrderId}", orderId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating order status" });
            }
        }

        /// <summary>
        /// Updates any order (admin override)
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">Order update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated order</returns>
        /// <response code="200">Order updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpPut("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> UpdateOrder(
            Guid orderId,
            [FromBody] OrderUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.OrderId != orderId)
                {
                    return BadRequest(new { message = "Order ID in URL and body must match" });
                }

                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} updating order: {OrderId}", adminId, orderId);

                var order = await _orderManagementService.UpdateOrderAsync(request, cancellationToken);

                _logger.LogInformation("Order updated successfully by admin: {OrderId}", orderId);

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid order update request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the order" });
            }
        }

        /// <summary>
        /// Soft deletes an order
        /// </summary>
        /// <param name="orderId">The order ID to delete</param>
        /// <param name="reason">Reason for deletion (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Order deleted successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpDelete("{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SoftDeleteOrder(
            Guid orderId,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} soft deleting order: {OrderId}", adminId, orderId);

                var result = await _orderManagementService.SoftDeleteOrderAsync(orderId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Order not found or already deleted" });
                }

                _logger.LogInformation("Order soft deleted successfully: {OrderId}", orderId);

                return Ok(new { message = "Order soft deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the order" });
            }
        }

        /// <summary>
        /// Permanently deletes an order (hard delete - IRREVERSIBLE)
        /// </summary>
        /// <param name="orderId">The order ID to permanently delete</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Order permanently deleted successfully</response>
        /// <response code="400">Reason is required</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpDelete("{orderId:guid}/hard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> HardDeleteOrder(
            Guid orderId,
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
                _logger.LogInformation("Admin {AdminId} hard deleting order: {OrderId} with reason: {Reason}",
                    adminId, orderId, reason);

                var result = await _orderManagementService.HardDeleteOrderAsync(orderId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Order not found" });
                }

                _logger.LogInformation("Order hard deleted successfully: {OrderId}", orderId);

                return Ok(new { message = "Order permanently deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while permanently deleting the order" });
            }
        }

        /// <summary>
        /// Restores a soft-deleted order
        /// </summary>
        /// <param name="orderId">The order ID to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restored order</returns>
        /// <response code="200">Order restored successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Order not found</response>
        [HttpPost("{orderId:guid}/restore")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> RestoreOrder(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} restoring order: {OrderId}", adminId, orderId);

                var order = await _orderManagementService.RestoreOrderAsync(orderId, adminId, cancellationToken);

                _logger.LogInformation("Order restored successfully: {OrderId}", orderId);

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order not found or not deleted: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while restoring the order" });
            }
        }

        /// <summary>
        /// Gets revenue analytics for a date range
        /// </summary>
        /// <param name="startDate">Start date (optional, defaults to 30 days ago)</param>
        /// <param name="endDate">End date (optional, defaults to today)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Revenue analytics data</returns>
        /// <response code="200">Returns the analytics</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("analytics/revenue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetRevenueAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                _logger.LogInformation("Admin fetching revenue analytics from {StartDate} to {EndDate}", start, end);

                var analytics = await _orderAnalyticsService.GetRevenueAnalyticsAsync(start, end, cancellationToken);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue analytics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching revenue analytics" });
            }
        }

        /// <summary>
        /// Gets order performance metrics for a date range
        /// </summary>
        /// <param name="startDate">Start date (optional, defaults to 30 days ago)</param>
        /// <param name="endDate">End date (optional, defaults to today)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order performance metrics</returns>
        /// <response code="200">Returns the metrics</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("analytics/performance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetOrderPerformanceMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                _logger.LogInformation("Admin fetching order performance metrics from {StartDate} to {EndDate}", start, end);

                var metrics = await _orderAnalyticsService.GetOrderPerformanceMetricsAsync(start, end, cancellationToken);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order performance metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching performance metrics" });
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
