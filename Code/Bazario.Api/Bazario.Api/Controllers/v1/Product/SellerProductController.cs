using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Product
{
    /// <summary>
    /// Seller API for managing their store's products
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seller/products")]
    [Authorize(Roles = "Seller")]
    [Tags("Seller - Products")]
    public class SellerProductController : ControllerBase
    {
        private readonly IProductManagementService _productManagementService;
        private readonly IProductQueryService _productQueryService;
        private readonly IProductAnalyticsService _productAnalyticsService;
        private readonly IStoreAuthorizationService _storeAuthorizationService;
        private readonly IStoreQueryService _storeQueryService;
        private readonly ILogger<SellerProductController> _logger;

        public SellerProductController(
            IProductManagementService productManagementService,
            IProductQueryService productQueryService,
            IProductAnalyticsService productAnalyticsService,
            IStoreAuthorizationService storeAuthorizationService,
            IStoreQueryService storeQueryService,
            ILogger<SellerProductController> logger)
        {
            _productManagementService = productManagementService;
            _productQueryService = productQueryService;
            _productAnalyticsService = productAnalyticsService;
            _storeAuthorizationService = storeAuthorizationService;
            _storeQueryService = storeQueryService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all products for the seller's store with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of seller's products</returns>
        /// <response code="200">Returns the seller's products</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not a seller or doesn't own a store</response>
        [HttpGet("my-products")]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<ProductResponse>>> GetMyProducts(
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

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { message = "Page size must be between 1 and 100" });
                }

                var sellerId = GetCurrentUserId();
                _logger.LogInformation("Fetching products for seller: {SellerId}", sellerId);

                // Get seller's stores
                var stores = await _storeQueryService.GetStoresBySellerIdAsync(sellerId, cancellationToken);

                if (stores == null || stores.Count == 0)
                {
                    _logger.LogWarning("Seller {SellerId} does not own a store", sellerId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You must own a store to view products" });
                }

                // Get first store (sellers typically have one store)
                var store = stores.First();

                var products = await _productQueryService.GetProductsByStoreIdAsync(
                    store.StoreId, pageNumber, pageSize, cancellationToken);

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching seller products");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching your products" });
            }
        }

        /// <summary>
        /// Gets products with low stock levels in the seller's store
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of products with low stock</returns>
        /// <response code="200">Returns products with low stock</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not a seller or doesn't own a store</response>
        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ProductResponse>>> GetLowStockProducts(
            [FromQuery] int threshold = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();
                _logger.LogInformation("Fetching low stock products for seller: {SellerId}, Threshold: {Threshold}",
                    sellerId, threshold);

                // Get seller's stores
                var stores = await _storeQueryService.GetStoresBySellerIdAsync(sellerId, cancellationToken);

                if (stores == null || stores.Count == 0)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You must own a store to view products" });
                }

                var storeIds = stores.Select(s => s.StoreId).ToList();

                var lowStockProducts = await _productQueryService.GetLowStockProductsAsync(threshold, cancellationToken);

                // Filter to only seller's products
                var sellerLowStockProducts = lowStockProducts.Where(p => storeIds.Contains(p.StoreId)).ToList();

                return Ok(sellerLowStockProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching low stock products");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching low stock products" });
            }
        }

        /// <summary>
        /// Creates a new product in the seller's store
        /// </summary>
        /// <param name="request">Product creation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created product</returns>
        /// <response code="201">Product created successfully</response>
        /// <response code="400">Invalid request or validation failed</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not authorized to create products for this store</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProductResponse>> CreateProduct(
            [FromBody] ProductAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, request.StoreId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to create product for unauthorized store {StoreId}",
                        sellerId, request.StoreId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to create products for this store" });
                }

                _logger.LogInformation("Creating product for store: {StoreId} by seller: {SellerId}",
                    request.StoreId, sellerId);

                var product = await _productManagementService.CreateProductAsync(request, cancellationToken);

                _logger.LogInformation("Product created successfully: {ProductId}", product.ProductId);

                return CreatedAtAction(
                    nameof(PublicProductController.GetProductById),
                    "PublicProduct",
                    new { productId = product.ProductId, version = "1.0" },
                    product);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product creation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the product" });
            }
        }

        /// <summary>
        /// Updates an existing product in the seller's store
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="request">Product update details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated product</returns>
        /// <response code="200">Product updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not authorized to update this product</response>
        /// <response code="404">Product not found</response>
        [HttpPut("{productId:guid}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

                var sellerId = GetCurrentUserId();

                // Get existing product to verify ownership
                var existingProduct = await _productQueryService.GetProductByIdAsync(productId, cancellationToken);

                if (existingProduct == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, existingProduct.StoreId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to update unauthorized product {ProductId}",
                        sellerId, productId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to update this product" });
                }

                _logger.LogInformation("Updating product: {ProductId} by seller: {SellerId}", productId, sellerId);

                var product = await _productManagementService.UpdateProductAsync(request, cancellationToken);

                _logger.LogInformation("Product updated successfully: {ProductId}", productId);

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
        /// Soft deletes a product (marks as deleted but preserves data)
        /// </summary>
        /// <param name="productId">The product ID to delete</param>
        /// <param name="reason">Optional reason for deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Product deleted successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not authorized to delete this product</response>
        /// <response code="404">Product not found</response>
        [HttpDelete("{productId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteProduct(
            Guid productId,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Get existing product to verify ownership
                var existingProduct = await _productQueryService.GetProductByIdAsync(productId, cancellationToken);

                if (existingProduct == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Verify seller can manage the store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, existingProduct.StoreId, cancellationToken);

                if (!canManage)
                {
                    _logger.LogWarning("Seller {SellerId} attempted to delete unauthorized product {ProductId}",
                        sellerId, productId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to delete this product" });
                }

                _logger.LogInformation("Deleting product: {ProductId} by seller: {SellerId}", productId, sellerId);

                var result = await _productManagementService.DeleteProductAsync(
                    productId, sellerId, reason, cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Product not found or already deleted" });
                }

                _logger.LogInformation("Product deleted successfully: {ProductId}", productId);

                return Ok(new { message = "Product deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", productId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the product" });
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
        /// <response code="403">User is not authorized to restore this product</response>
        /// <response code="404">Product not found</response>
        [HttpPost("{productId:guid}/restore")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> RestoreProduct(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sellerId = GetCurrentUserId();

                // Note: We can't check existing product first since it's deleted
                // The service will handle authorization checks
                _logger.LogInformation("Restoring product: {ProductId} by seller: {SellerId}", productId, sellerId);

                var product = await _productManagementService.RestoreProductAsync(productId, sellerId, cancellationToken);

                // Verify seller can manage the restored product's store
                var canManage = await _storeAuthorizationService.CanUserManageStoreAsync(
                    sellerId, product.StoreId, cancellationToken);

                if (!canManage)
                {
                    // If not authorized, delete it again
                    await _productManagementService.DeleteProductAsync(productId, sellerId, "Unauthorized restore attempt", cancellationToken);

                    _logger.LogWarning("Seller {SellerId} attempted to restore unauthorized product {ProductId}",
                        sellerId, productId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You are not authorized to restore this product" });
                }

                _logger.LogInformation("Product restored successfully: {ProductId}", productId);

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
