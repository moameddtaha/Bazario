using Asp.Versioning;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Order
{
    /// <summary>
    /// Customer API for managing their own orders
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customer/orders")]
    [Authorize(Roles = "Customer")]
    [Tags("Customer - Orders")]
    public class CustomerOrderController : ControllerBase
    {
        private readonly IOrderManagementService _orderManagementService;
        private readonly IOrderQueryService _orderQueryService;
        private readonly IOrderAnalyticsService _orderAnalyticsService;
        private readonly ILogger<CustomerOrderController> _logger;

        public CustomerOrderController(
            IOrderManagementService orderManagementService,
            IOrderQueryService orderQueryService,
            IOrderAnalyticsService orderAnalyticsService,
            ILogger<CustomerOrderController> logger)
        {
            _orderManagementService = orderManagementService;
            _orderQueryService = orderQueryService;
            _orderAnalyticsService = orderAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all orders for the current customer
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer's orders</returns>
        /// <response code="200">Returns the customer's orders</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("my-orders")]
        [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<OrderResponse>>> GetMyOrders(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();
                _logger.LogInformation("Fetching orders for customer: {CustomerId}", customerId);

                var orders = await _orderQueryService.GetOrdersByCustomerIdAsync(customerId, cancellationToken);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer orders");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching your orders" });
            }
        }

        /// <summary>
        /// Gets a specific order by ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order details</returns>
        /// <response code="200">Returns the order</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to view this order</response>
        /// <response code="404">Order not found</response>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> GetOrderById(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();
                _logger.LogInformation("Customer {CustomerId} fetching order: {OrderId}", customerId, orderId);

                var order = await _orderQueryService.GetOrderByIdAsync(orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { message = "Order not found" });
                }

                // Verify the order belongs to the customer
                if (order.CustomerId != customerId)
                {
                    _logger.LogWarning("Customer {CustomerId} attempted to access unauthorized order {OrderId}",
                        customerId, orderId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to view this order" });
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
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created order</returns>
        /// <response code="201">Order created successfully</response>
        /// <response code="400">Invalid request or validation failed</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<OrderResponse>> CreateOrder(
            [FromBody] OrderAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();

                if (request.CustomerId != customerId)
                {
                    _logger.LogWarning("Customer ID mismatch. Token: {TokenId}, Request: {RequestId}",
                        customerId, request.CustomerId);
                    return BadRequest(new { message = "You can only create orders for yourself" });
                }

                _logger.LogInformation("Creating order for customer: {CustomerId}", customerId);

                var order = await _orderManagementService.CreateOrderAsync(request, cancellationToken);

                _logger.LogInformation("Order created successfully: {OrderId}", order.OrderId);

                return CreatedAtAction(
                    nameof(GetOrderById),
                    new { orderId = order.OrderId, version = "1.0" },
                    order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid order creation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order creation failed due to business rule");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the order" });
            }
        }

        /// <summary>
        /// Updates an existing order (only allowed for Pending orders)
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">Order update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated order</returns>
        /// <response code="200">Order updated successfully</response>
        /// <response code="400">Invalid request or order cannot be updated</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to update this order</response>
        /// <response code="404">Order not found</response>
        [HttpPut("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

                var customerId = GetCurrentUserId();

                // Get existing order to verify ownership
                var existingOrder = await _orderQueryService.GetOrderByIdAsync(orderId, cancellationToken);

                if (existingOrder == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (existingOrder.CustomerId != customerId)
                {
                    _logger.LogWarning("Customer {CustomerId} attempted to update unauthorized order {OrderId}",
                        customerId, orderId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to update this order" });
                }

                _logger.LogInformation("Updating order: {OrderId} by customer: {CustomerId}", orderId, customerId);

                var order = await _orderManagementService.UpdateOrderAsync(request, cancellationToken);

                _logger.LogInformation("Order updated successfully: {OrderId}", orderId);

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order update not allowed: {OrderId}", orderId);
                return BadRequest(new { message = ex.Message });
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
        /// Cancels an order (only allowed for Pending/Processing orders)
        /// </summary>
        /// <param name="orderId">The order ID to cancel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Order cancelled successfully</response>
        /// <response code="400">Order cannot be cancelled</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized to cancel this order</response>
        /// <response code="404">Order not found</response>
        [HttpPost("{orderId:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CancelOrder(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();

                // Get existing order to verify ownership
                var existingOrder = await _orderQueryService.GetOrderByIdAsync(orderId, cancellationToken);

                if (existingOrder == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (existingOrder.CustomerId != customerId)
                {
                    _logger.LogWarning("Customer {CustomerId} attempted to cancel unauthorized order {OrderId}",
                        customerId, orderId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to cancel this order" });
                }

                _logger.LogInformation("Cancelling order: {OrderId} by customer: {CustomerId}", orderId, customerId);

                var result = await _orderManagementService.CancelOrderAsync(orderId, cancellationToken);

                if (!result)
                {
                    return BadRequest(new { message = "Order could not be cancelled" });
                }

                _logger.LogInformation("Order cancelled successfully: {OrderId}", orderId);

                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order cancellation not allowed: {OrderId}", orderId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while cancelling the order" });
            }
        }

        /// <summary>
        /// Gets order analytics for the current customer
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer order analytics</returns>
        /// <response code="200">Returns the analytics</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("my-analytics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMyOrderAnalytics(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customerId = GetCurrentUserId();
                _logger.LogInformation("Fetching order analytics for customer: {CustomerId}", customerId);

                var analytics = await _orderAnalyticsService.GetCustomerOrderAnalyticsAsync(customerId, cancellationToken);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer order analytics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching order analytics" });
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
