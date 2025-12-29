using Asp.Versioning;
using Bazario.Core.DTO.Store;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Store
{
    /// <summary>
    /// Public API for browsing stores
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/stores")]
    [Tags("Public - Stores")]
    public class PublicStoreController : ControllerBase
    {
        private readonly IStoreQueryService _storeQueryService;
        private readonly IStoreAnalyticsService _storeAnalyticsService;
        private readonly ILogger<PublicStoreController> _logger;

        public PublicStoreController(
            IStoreQueryService storeQueryService,
            IStoreAnalyticsService storeAnalyticsService,
            ILogger<PublicStoreController> logger)
        {
            _storeQueryService = storeQueryService;
            _storeAnalyticsService = storeAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a store by ID with its details
        /// </summary>
        /// <param name="storeId">The store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store details</returns>
        /// <response code="200">Returns the store</response>
        /// <response code="404">Store not found</response>
        [HttpGet("{storeId:guid}")]
        [ProducesResponseType(typeof(StoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoreResponse>> GetStoreById(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching store: {StoreId}", storeId);

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
        /// Searches stores with filtering and pagination
        /// </summary>
        /// <param name="searchTerm">Search term for store name</param>
        /// <param name="category">Filter by category</param>
        /// <param name="isActive">Filter by active status (default: true)</param>
        /// <param name="sortBy">Sort field (Name, CreatedAt, Rating, Revenue)</param>
        /// <param name="sortDescending">Sort descending (default: false)</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of stores</returns>
        /// <response code="200">Returns matching stores</response>
        /// <response code="400">Invalid search parameters</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResponse<StoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<StoreResponse>>> SearchStores(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = true,
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
                    "Searching stores with term: {SearchTerm}, Category: {Category}, Page: {PageNumber}",
                    searchTerm, category, pageNumber);

                var searchCriteria = new StoreSearchCriteria
                {
                    SearchTerm = searchTerm,
                    Category = category,
                    IsActive = isActive,
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
        /// Gets stores by category with pagination
        /// </summary>
        /// <param name="category">Category to filter by</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of stores in the category</returns>
        /// <response code="200">Returns stores in the category</response>
        /// <response code="400">Invalid parameters</response>
        [HttpGet("category/{category}")]
        [ProducesResponseType(typeof(PagedResponse<StoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<StoreResponse>>> GetStoresByCategory(
            string category,
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

                _logger.LogInformation("Fetching stores in category: {Category}, Page: {PageNumber}",
                    category, pageNumber);

                var searchCriteria = new StoreSearchCriteria
                {
                    Category = category,
                    IsActive = true,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var stores = await _storeQueryService.GetStoresByCategoryAsync(searchCriteria, cancellationToken);

                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores by category: {Category}", category);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching stores" });
            }
        }

        /// <summary>
        /// Gets top performing stores
        /// </summary>
        /// <param name="performanceCriteria">Performance criteria (Revenue, OrderCount, Rating)</param>
        /// <param name="category">Optional category filter</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 50)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Top performing stores</returns>
        /// <response code="200">Returns top performing stores</response>
        /// <response code="400">Invalid parameters</response>
        [HttpGet("top-performing")]
        [ProducesResponseType(typeof(PagedResponse<StorePerformance>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<StorePerformance>>> GetTopPerformingStores(
            [FromQuery] string performanceCriteria = "Revenue",
            [FromQuery] string? category = null,
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

                _logger.LogInformation("Fetching top performing stores by: {Criteria}", performanceCriteria);

                var searchCriteria = new StoreSearchCriteria
                {
                    Category = category,
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
    }
}
