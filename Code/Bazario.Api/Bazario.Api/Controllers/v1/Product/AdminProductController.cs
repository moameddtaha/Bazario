using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Product
{
    /// <summary>
    /// Admin API for product management and oversight
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/products")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Products")]
    public class AdminProductController : ControllerBase
    {
        private readonly IProductManagementService _productManagementService;
        private readonly IProductQueryService _productQueryService;
        private readonly IProductAnalyticsService _productAnalyticsService;
        private readonly ILogger<AdminProductController> _logger;

        public AdminProductController(
            IProductManagementService productManagementService,
            IProductQueryService productQueryService,
            IProductAnalyticsService productAnalyticsService,
            ILogger<AdminProductController> logger)
        {
            _productManagementService = productManagementService;
            _productQueryService = productQueryService;
            _productAnalyticsService = productAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all products with low stock across all stores
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of products with low stock</returns>
        /// <response code="200">Returns products with low stock</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ProductResponse>>> GetLowStockProducts(
            [FromQuery] int threshold = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching low stock products with threshold: {Threshold}", threshold);

                var lowStockProducts = await _productQueryService.GetLowStockProductsAsync(threshold, cancellationToken);

                return Ok(lowStockProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching low stock products");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching low stock products" });
            }
        }

        /// <summary>
        /// Gets product analytics for any product (admin view)
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product analytics data</returns>
        /// <response code="200">Returns the product analytics</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{productId:guid}/analytics")]
        [ProducesResponseType(typeof(ProductAnalytics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductAnalytics>> GetProductAnalytics(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Admin fetching analytics for product: {ProductId}", productId);

                var analytics = await _productAnalyticsService.GetProductAnalyticsAsync(productId, cancellationToken);

                return Ok(analytics);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = "Product not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product analytics: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching product analytics" });
            }
        }

        /// <summary>
        /// Updates any product (admin override)
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="request">Product update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated product</returns>
        /// <response code="200">Product updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Product not found</response>
        [HttpPut("{productId:guid}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> UpdateProduct(
            Guid productId,
            [FromBody] ProductUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.ProductId != productId)
                {
                    return BadRequest(new { message = "Product ID in URL and body must match" });
                }

                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} updating product: {ProductId}", adminId, productId);

                var product = await _productManagementService.UpdateProductAsync(request, cancellationToken);

                _logger.LogInformation("Product updated successfully by admin: {ProductId}", productId);

                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product update request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the product" });
            }
        }

        /// <summary>
        /// Soft deletes any product (admin override)
        /// </summary>
        /// <param name="productId">The product ID to delete</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Product deleted successfully</response>
        /// <response code="400">Reason is required</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Product not found</response>
        [HttpDelete("{productId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SoftDeleteProduct(
            Guid productId,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} soft deleting product: {ProductId}", adminId, productId);

                var result = await _productManagementService.DeleteProductAsync(
                    productId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Product not found or already deleted" });
                }

                _logger.LogInformation("Product soft deleted by admin: {ProductId}", productId);

                return Ok(new { message = "Product soft deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the product" });
            }
        }

        /// <summary>
        /// Permanently deletes a product (hard delete - requires admin and no active orders)
        /// </summary>
        /// <param name="productId">The product ID to permanently delete</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Product permanently deleted successfully</response>
        /// <response code="400">Reason is required or product has active orders</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Product not found</response>
        [HttpDelete("{productId:guid}/hard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> HardDeleteProduct(
            Guid productId,
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

                _logger.LogInformation("Admin {AdminId} hard deleting product: {ProductId} with reason: {Reason}",
                    adminId, productId, reason);

                var result = await _productManagementService.HardDeleteProductAsync(
                    productId, adminId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Product not found" });
                }

                _logger.LogInformation("Product hard deleted by admin: {ProductId}", productId);

                return Ok(new { message = "Product permanently deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot hard delete product with active orders: {ProductId}", productId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while permanently deleting the product" });
            }
        }

        /// <summary>
        /// Restores a soft-deleted product
        /// </summary>
        /// <param name="productId">The product ID to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restored product</returns>
        /// <response code="200">Product restored successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User not authorized</response>
        /// <response code="404">Product not found</response>
        [HttpPost("{productId:guid}/restore")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> RestoreProduct(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();

                _logger.LogInformation("Admin {AdminId} restoring product: {ProductId}", adminId, productId);

                var product = await _productManagementService.RestoreProductAsync(productId, adminId, cancellationToken);

                _logger.LogInformation("Product restored by admin: {ProductId}", productId);

                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while restoring the product" });
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
