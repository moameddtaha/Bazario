using Asp.Versioning;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Product
{
    /// <summary>
    /// Public API for browsing and searching products (no authentication required)
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/products")]
    [Tags("Public - Products")]
    public class PublicProductController : ControllerBase
    {
        private readonly IProductQueryService _productQueryService;
        private readonly IProductAnalyticsService _productAnalyticsService;
        private readonly ILogger<PublicProductController> _logger;

        public PublicProductController(
            IProductQueryService productQueryService,
            IProductAnalyticsService productAnalyticsService,
            ILogger<PublicProductController> logger)
        {
            _productQueryService = productQueryService;
            _productAnalyticsService = productAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a specific product by ID
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product details</returns>
        /// <response code="200">Returns the product</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{productId:guid}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> GetProductById(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching product: {ProductId}", productId);

                var product = await _productQueryService.GetProductByIdAsync(productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", productId);
                    return NotFound(new { message = "Product not found" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product: {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the product" });
            }
        }

        /// <summary>
        /// Gets all products for a specific store with pagination
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 50)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of store products</returns>
        /// <response code="200">Returns the paginated products</response>
        /// <response code="400">Invalid parameters</response>
        [HttpGet("store/{storeId:guid}")]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<ProductResponse>>> GetStoreProducts(
            Guid storeId,
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

                _logger.LogInformation(
                    "Fetching products for store: {StoreId}, Page: {PageNumber}, Size: {PageSize}",
                    storeId, pageNumber, pageSize);

                var products = await _productQueryService.GetProductsByStoreIdAsync(
                    storeId, pageNumber, pageSize, cancellationToken);

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store products: {StoreId}", storeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching store products" });
            }
        }

        /// <summary>
        /// Searches products with advanced filtering and pagination
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria including keyword, category, price range, and sorting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        /// <response code="200">Returns the search results</response>
        /// <response code="400">Invalid search criteria</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<ProductResponse>>> SearchProducts(
            [FromBody] ProductSearchCriteria searchCriteria,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (searchCriteria == null)
                {
                    return BadRequest(new { message = "Search criteria cannot be null" });
                }

                if (searchCriteria.PageNumber < 1)
                {
                    return BadRequest(new { message = "Page number must be greater than 0" });
                }

                if (searchCriteria.PageSize < 1 || searchCriteria.PageSize > 50)
                {
                    return BadRequest(new { message = "Page size must be between 1 and 50" });
                }

                _logger.LogInformation(
                    "Searching products with keyword: {Keyword}, Category: {Category}, Page: {PageNumber}",
                    searchCriteria.Keyword, searchCriteria.Category, searchCriteria.PageNumber);

                var results = await _productQueryService.SearchProductsAsync(searchCriteria, cancellationToken);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while searching products" });
            }
        }

        /// <summary>
        /// Gets product analytics including sales statistics and trends
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product analytics data</returns>
        /// <response code="200">Returns the product analytics</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{productId:guid}/analytics")]
        [ProducesResponseType(typeof(ProductAnalytics), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductAnalytics>> GetProductAnalytics(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching analytics for product: {ProductId}", productId);

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
    }
}
