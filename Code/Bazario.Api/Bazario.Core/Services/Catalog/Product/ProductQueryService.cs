using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Extensions.Catalog;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product query operations
    /// Thread-safe: All dependencies are injected and fields are readonly
    /// Lifecycle: Scoped (recommended) or Singleton (safe but may hold DB context longer)
    /// </summary>
    public class ProductQueryService : IProductQueryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductQueryService> _logger;
        private readonly IConfigurationHelper _configHelper;
        private readonly int _maximumPageSize;
        private readonly int _maximumStockThreshold;

        // Default configuration values
        /// <summary>
        /// Minimum valid page number (1-based pagination)
        /// </summary>
        private const int MINIMUM_PAGE_NUMBER = 1;

        /// <summary>
        /// Minimum items per page (prevents invalid pagination)
        /// </summary>
        private const int MINIMUM_PAGE_SIZE = 1;

        /// <summary>
        /// Default maximum page size to prevent DoS attacks via large page requests
        /// Based on: typical UI pagination (25-100 items) and database query performance
        /// </summary>
        private const int DEFAULT_MAXIMUM_PAGE_SIZE = 100;

        /// <summary>
        /// Absolute maximum page size regardless of configuration (prevents DoS attacks)
        /// Hard limit to prevent misconfiguration from allowing excessive page sizes
        /// </summary>
        private const int ABSOLUTE_MAXIMUM_PAGE_SIZE = 1000;

        /// <summary>
        /// Maximum search term length to prevent DoS attacks and log injection
        /// Based on: reasonable product name/description search queries
        /// </summary>
        private const int MAXIMUM_SEARCH_TERM_LENGTH = 200;

        /// <summary>
        /// Default low stock threshold for inventory alerts
        /// Based on: typical reorder lead time of 1-2 weeks
        /// </summary>
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;

        /// <summary>
        /// Default maximum stock threshold to prevent unrealistic values in queries
        /// Based on: typical warehouse capacity constraints
        /// </summary>
        private const int DEFAULT_MAXIMUM_STOCK_THRESHOLD = 10000;

        /// <summary>
        /// Number format for currency/price logging (2 decimal places)
        /// </summary>
        private const string PRICE_LOG_FORMAT = "N2";

        /// <summary>
        /// Placeholder text for products without a name (used in logging)
        /// </summary>
        private const string UNNAMED_PRODUCT_PLACEHOLDER = "[Unnamed]";

        // Configuration keys
        private static class ConfigurationKeys
        {
            public const string MaximumPageSize = "Validation:MaximumPageSize";
            public const string MaximumStockThreshold = "Validation:MaximumStockThreshold";
        }

        // Error message constants for consistency and potential localization
        private static class ErrorMessages
        {
            public const string SearchCriteriaMissing = "Search criteria cannot be null";
            public const string NoSearchCriteriaProvided = "At least one search criterion (SearchTerm, StoreId, Category, MinPrice, or MaxPrice) must be provided";
            public const string InvalidPageNumber = "Page number must be at least {0}";
            public const string InvalidPageSize = "Page size must be between {0} and {1}";
            public const string NegativeMinPrice = "Minimum price cannot be negative";
            public const string NegativeMaxPrice = "Maximum price cannot be negative";
            public const string MinPriceExceedsMaxPrice = "Minimum price cannot be greater than maximum price";
            public const string ProductIdEmpty = "Product ID cannot be empty";
            public const string StoreIdEmpty = "Store ID cannot be empty";
            public const string NegativeThreshold = "Threshold must be non-negative";
            public const string ThresholdExceedsMaximum = "Threshold cannot exceed {0}";
            public const string SearchTermTooLong = "Search term cannot exceed {0} characters";
            public const string InvalidCategoryValue = "Invalid category value: {0}";
            public const string ConflictingDeletionFilters = "Cannot specify both OnlyDeleted and IncludeDeleted. Use OnlyDeleted to search only deleted products, or IncludeDeleted to search both active and deleted products.";
            public const string RatingSortDisabled = "Rating sort is temporarily disabled due to performance concerns. " +
                "To enable rating sort, add an AverageRating computed column to the Product entity. " +
                "Please use 'name', 'price', or 'createdat' for sorting.";
        }

        // Sort field constants
        private static class SortFields
        {
            public const string Name = "name";
            public const string Price = "price";
            public const string CreatedAt = "createdat";
            public const string Rating = "rating";
        }

        public ProductQueryService(
            IProductRepository productRepository,
            ILogger<ProductQueryService> logger,
            IConfigurationHelper configHelper)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configHelper = configHelper ?? throw new ArgumentNullException(nameof(configHelper));

            // Load configurable thresholds with defaults (aligned with ProductValidationService pattern)
            _maximumPageSize = _configHelper.GetValue(ConfigurationKeys.MaximumPageSize, DEFAULT_MAXIMUM_PAGE_SIZE);
            _maximumStockThreshold = _configHelper.GetValue(ConfigurationKeys.MaximumStockThreshold, DEFAULT_MAXIMUM_STOCK_THRESHOLD);

            // Validate configuration values to prevent invalid runtime behavior and DoS attacks
            if (_maximumPageSize < MINIMUM_PAGE_SIZE)
            {
                _logger.LogWarning("Invalid configuration: MaximumPageSize ({MaximumPageSize}) is less than minimum ({MinimumPageSize}). Using default: {Default}",
                    _maximumPageSize, MINIMUM_PAGE_SIZE, DEFAULT_MAXIMUM_PAGE_SIZE);
                _maximumPageSize = DEFAULT_MAXIMUM_PAGE_SIZE;
            }
            else if (_maximumPageSize > ABSOLUTE_MAXIMUM_PAGE_SIZE)
            {
                _logger.LogWarning("Invalid configuration: MaximumPageSize ({MaximumPageSize}) exceeds absolute maximum ({AbsoluteMaximum}). Using absolute maximum.",
                    _maximumPageSize, ABSOLUTE_MAXIMUM_PAGE_SIZE);
                _maximumPageSize = ABSOLUTE_MAXIMUM_PAGE_SIZE;
            }

            if (_maximumStockThreshold < 0)
            {
                _logger.LogWarning("Invalid configuration: MaximumStockThreshold ({MaximumStockThreshold}) is negative. Using default: {Default}",
                    _maximumStockThreshold, DEFAULT_MAXIMUM_STOCK_THRESHOLD);
                _maximumStockThreshold = DEFAULT_MAXIMUM_STOCK_THRESHOLD;
            }
        }

        /// <summary>
        /// Retrieves a product by ID with complete details
        /// </summary>
        /// <param name="productId">Product ID to retrieve (must not be empty GUID)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Product response if found; null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when productId is empty GUID</exception>
        /// <exception cref="InvalidOperationException">Thrown when product-to-response conversion fails</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            // Validate input BEFORE starting stopwatch
            if (productId == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Retrieving product by ID: {ProductId}", productId);

            try
            {
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogDebug("Product not found: {ProductId} (completed in {ElapsedMs}ms)", productId, stopwatch.ElapsedMilliseconds);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved product: {ProductId}, Name: {ProductName} (completed in {ElapsedMs}ms)",
                    product.ProductId, product.Name ?? UNNAMED_PRODUCT_PLACEHOLDER, stopwatch.ElapsedMilliseconds);

                return product.ToProductResponse()
                    ?? throw new InvalidOperationException($"Failed to convert product {productId} to response");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Product retrieval cancelled: {ProductId} (cancelled after {ElapsedMs}ms)", productId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product: {ProductId} (failed after {ElapsedMs}ms)", productId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Retrieves paginated products for a specific store
        /// </summary>
        /// <param name="storeId">Store ID (must not be empty GUID)</param>
        /// <param name="pageNumber">Page number (1-based, minimum: 1)</param>
        /// <param name="pageSize">Items per page (minimum: 1, maximum: configured limit)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated response containing store products with metadata</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty, pageNumber &lt; 1, or pageSize is out of bounds</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public async Task<PagedResponse<ProductResponse>> GetProductsByStoreIdAsync(Guid storeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            // Validate input BEFORE starting stopwatch
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.StoreIdEmpty, nameof(storeId));
            }

            // Validate pagination parameters to prevent DoS attacks and invalid queries
            if (pageNumber < MINIMUM_PAGE_NUMBER)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.InvalidPageNumber, MINIMUM_PAGE_NUMBER),
                    nameof(pageNumber));
            }

            if (pageSize < MINIMUM_PAGE_SIZE || pageSize > _maximumPageSize)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.InvalidPageSize, MINIMUM_PAGE_SIZE, _maximumPageSize),
                    nameof(pageSize));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Retrieving products for store: {StoreId}, Page: {PageNumber}, PageSize: {PageSize}",
                storeId, pageNumber, pageSize);

            try
            {
                // Use AsNoTracking queryable for read-only operations (performance optimization)
                var query = _productRepository.GetProductsQueryableAsNoTracking()
                    .Where(p => p.StoreId == storeId);

                // Check for cancellation before executing expensive count query
                cancellationToken.ThrowIfCancellationRequested();

                // Get total count for pagination metadata
                var totalCount = await _productRepository.GetProductsCountAsync(query, cancellationToken);

                // Get paginated results
                var products = await _productRepository.GetProductsPagedAsync(query, pageNumber, pageSize, cancellationToken);

                _logger.LogInformation("Successfully retrieved {Count} of {TotalCount} products for store: {StoreId}, Page: {PageNumber} (completed in {ElapsedMs}ms)",
                    products.Count, totalCount, storeId, pageNumber, stopwatch.ElapsedMilliseconds);

                var productResponses = products.Select(p => p.ToProductResponse()).OfType<ProductResponse>().ToList();

                return new PagedResponse<ProductResponse>
                {
                    Items = productResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Products retrieval for store cancelled: {StoreId} (cancelled after {ElapsedMs}ms)", storeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products for store: {StoreId} (failed after {ElapsedMs}ms)", storeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Searches products with advanced filtering and pagination
        /// At least one search criterion must be provided (SearchTerm, StoreId, Category, MinPrice, or MaxPrice)
        /// Note: Products with null Category can only be found through other search criteria (SearchTerm, StoreId, Price range)
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria (must not be null)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated search results with matching products</returns>
        /// <exception cref="ArgumentNullException">Thrown when searchCriteria is null</exception>
        /// <exception cref="ArgumentException">Thrown when validation fails (invalid pagination, price range, category, search term length, or conflicting filters)</exception>
        /// <exception cref="NotSupportedException">Thrown when rating sort is requested (not yet implemented)</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public async Task<PagedResponse<ProductResponse>> SearchProductsAsync(ProductSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            // Validate input BEFORE starting stopwatch (consistent with ProductManagementService pattern)
            if (searchCriteria == null)
            {
                throw new ArgumentNullException(nameof(searchCriteria), ErrorMessages.SearchCriteriaMissing);
            }

            // Validate pagination parameters to prevent DoS attacks and invalid queries
            if (searchCriteria.PageNumber < MINIMUM_PAGE_NUMBER)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.InvalidPageNumber, MINIMUM_PAGE_NUMBER),
                    nameof(searchCriteria));
            }

            if (searchCriteria.PageSize < MINIMUM_PAGE_SIZE || searchCriteria.PageSize > _maximumPageSize)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.InvalidPageSize, MINIMUM_PAGE_SIZE, _maximumPageSize),
                    nameof(searchCriteria));
            }

            // Validate price range to prevent invalid queries
            if (searchCriteria.MinPrice.HasValue && searchCriteria.MinPrice.Value < 0)
            {
                throw new ArgumentException(ErrorMessages.NegativeMinPrice, nameof(searchCriteria));
            }

            if (searchCriteria.MaxPrice.HasValue && searchCriteria.MaxPrice.Value < 0)
            {
                throw new ArgumentException(ErrorMessages.NegativeMaxPrice, nameof(searchCriteria));
            }

            if (searchCriteria.MinPrice.HasValue && searchCriteria.MaxPrice.HasValue &&
                searchCriteria.MinPrice.Value > searchCriteria.MaxPrice.Value)
            {
                throw new ArgumentException(ErrorMessages.MinPriceExceedsMaxPrice, nameof(searchCriteria));
            }

            // Validate StoreId is not empty Guid if provided
            if (searchCriteria.StoreId.HasValue && searchCriteria.StoreId.Value == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.StoreIdEmpty, nameof(searchCriteria));
            }

            // Validate Category enum is defined if provided
            if (searchCriteria.Category.HasValue && !Enum.IsDefined(typeof(Category), searchCriteria.Category.Value))
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.InvalidCategoryValue, searchCriteria.Category.Value),
                    nameof(searchCriteria));
            }

            // Validate SearchTerm length to prevent DoS attacks and log injection
            if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm) && searchCriteria.SearchTerm.Length > MAXIMUM_SEARCH_TERM_LENGTH)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.SearchTermTooLong, MAXIMUM_SEARCH_TERM_LENGTH),
                    nameof(searchCriteria));
            }

            // Validate conflicting deletion filters
            if (searchCriteria.OnlyDeleted && searchCriteria.IncludeDeleted)
            {
                throw new ArgumentException(ErrorMessages.ConflictingDeletionFilters, nameof(searchCriteria));
            }

            // Business Rule: At least one search filter required to prevent accidental full table scans
            if (string.IsNullOrWhiteSpace(searchCriteria.SearchTerm) &&
                !searchCriteria.StoreId.HasValue &&
                !searchCriteria.Category.HasValue &&
                !searchCriteria.MinPrice.HasValue &&
                !searchCriteria.MaxPrice.HasValue)
            {
                throw new ArgumentException(ErrorMessages.NoSearchCriteriaProvided, nameof(searchCriteria));
            }

            // Start stopwatch AFTER all validation (consistent with ProductManagementService)
            var stopwatch = Stopwatch.StartNew();

            // Use structured logging for search criteria
            _logger.LogDebug("Searching products with criteria: {@SearchCriteria}", new
            {
                SearchTerm = SanitizeForLogging(searchCriteria.SearchTerm),
                searchCriteria.Category,
                searchCriteria.MinPrice,
                searchCriteria.MaxPrice,
                searchCriteria.StoreId,
                searchCriteria.InStockOnly,
                searchCriteria.OnlyDeleted,
                searchCriteria.IncludeDeleted,
                searchCriteria.SortBy,
                searchCriteria.SortDescending,
                searchCriteria.PageNumber,
                searchCriteria.PageSize
            });

            try
            {
                // Start with IQueryable - stays as SQL
                // Use AsNoTracking for read-only queries (performance optimization)
                var query = _productRepository.GetProductsQueryableAsNoTracking();

                // Apply soft deletion filters (these become SQL WHERE clauses)
                if (searchCriteria.OnlyDeleted)
                {
                    // Need to ignore the global filter to get deleted products
                    query = _productRepository.GetProductsQueryableIgnoreFiltersAsNoTracking().Where(p => p.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    // Need to ignore the global filter to include both active and deleted products
                    query = _productRepository.GetProductsQueryableIgnoreFiltersAsNoTracking();
                }
                // If neither OnlyDeleted nor IncludeDeleted, the global HasQueryFilter
                // automatically applies !p.IsDeleted, so no additional filter needed

                // Apply filters (these become SQL WHERE clauses)
                if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
                {
                    // Trim search term and use case-insensitive comparison (EF Core 9.0+ translates this to SQL COLLATE)
                    var searchTerm = searchCriteria.SearchTerm.Trim();
                    query = query.Where(p =>
                        (p.Name != null && p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                if (searchCriteria.StoreId.HasValue)
                {
                    query = query.Where(p => p.StoreId == searchCriteria.StoreId.Value);
                }

                if (searchCriteria.Category.HasValue)
                {
                    // Case-insensitive category comparison for better UX (EF Core 9.0+ translates this to SQL COLLATE)
                    var categoryString = searchCriteria.Category.Value.ToString();
                    query = query.Where(p => p.Category != null && p.Category.Equals(categoryString, StringComparison.OrdinalIgnoreCase));
                }

                if (searchCriteria.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= searchCriteria.MinPrice.Value);
                }

                if (searchCriteria.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= searchCriteria.MaxPrice.Value);
                }

                if (searchCriteria.InStockOnly == true)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Apply sorting (this becomes SQL ORDER BY)
                // NOTE: Rating sort is disabled due to N+1 query performance issues
                // Computing Average(r => r.Rating) in ORDER BY can load all reviews into memory
                query = searchCriteria.SortBy?.ToLower() switch
                {
                    SortFields.Name => searchCriteria.SortDescending ?
                        query.OrderByDescending(p => p.Name) :
                        query.OrderBy(p => p.Name),
                    SortFields.Price => searchCriteria.SortDescending ?
                        query.OrderByDescending(p => p.Price) :
                        query.OrderBy(p => p.Price),
                    SortFields.CreatedAt => searchCriteria.SortDescending ?
                        query.OrderByDescending(p => p.CreatedAt) :
                        query.OrderBy(p => p.CreatedAt),
                    SortFields.Rating => throw new NotSupportedException(ErrorMessages.RatingSortDisabled),
                    _ => query.OrderBy(p => p.Name)
                };

                cancellationToken.ThrowIfCancellationRequested();

                var totalCount = await _productRepository.GetProductsCountAsync(query, cancellationToken);

                // Check for cancellation before executing expensive paged query
                cancellationToken.ThrowIfCancellationRequested();

                var products = await _productRepository.GetProductsPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                var productResponses = products.Select(p => p.ToProductResponse()).OfType<ProductResponse>().ToList();

                var result = new PagedResponse<ProductResponse>
                {
                    Items = productResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogInformation(
                    "Product search completed in {ElapsedMs}ms: Found {TotalCount} products, Page {PageNumber}/{TotalPages} with {ItemCount} items",
                    stopwatch.ElapsedMilliseconds, totalCount, searchCriteria.PageNumber, result.TotalPages, productResponses.Count);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Product search cancelled after {ElapsedMs}ms with criteria: {SearchTerm}",
                    stopwatch.ElapsedMilliseconds, SanitizeForLogging(searchCriteria.SearchTerm));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Product search failed after {ElapsedMs}ms with criteria: {SearchTerm}",
                    stopwatch.ElapsedMilliseconds, SanitizeForLogging(searchCriteria.SearchTerm));
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Retrieves products with low stock levels (stock quantity &lt;= threshold)
        /// </summary>
        /// <param name="threshold">Stock quantity threshold (default: 10, must be non-negative and not exceed configured maximum)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>List of products with stock quantity at or below the threshold</returns>
        /// <exception cref="ArgumentException">Thrown when threshold is negative or exceeds configured maximum</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public async Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = DEFAULT_LOW_STOCK_THRESHOLD, CancellationToken cancellationToken = default)
        {
            // Validate input BEFORE starting stopwatch
            if (threshold < 0)
            {
                throw new ArgumentException(ErrorMessages.NegativeThreshold, nameof(threshold));
            }

            if (threshold > _maximumStockThreshold)
            {
                throw new ArgumentException(
                    string.Format(ErrorMessages.ThresholdExceedsMaximum, _maximumStockThreshold),
                    nameof(threshold));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Retrieving products with low stock (threshold: {Threshold})", threshold);

            try
            {
                var products = await _productRepository.GetLowStockProductsAsync(threshold, cancellationToken);

                _logger.LogInformation("Successfully found {Count} products with low stock (threshold: {Threshold}) (completed in {ElapsedMs}ms)",
                    products.Count, threshold, stopwatch.ElapsedMilliseconds);

                return products.Select(p => p.ToProductResponse()).OfType<ProductResponse>().ToList();;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Low stock products retrieval cancelled (threshold: {Threshold}) (cancelled after {ElapsedMs}ms)",
                    threshold, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve low stock products with threshold: {Threshold} (failed after {ElapsedMs}ms)",
                    threshold, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Sanitizes input strings for safe logging (prevents log injection and truncates long strings)
        /// </summary>
        /// <param name="input">Input string to sanitize</param>
        /// <param name="maxLength">Maximum length (default 100 characters)</param>
        /// <returns>Sanitized string safe for logging</returns>
        private static string SanitizeForLogging(string? input, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(input)) return "N/A";

            // Remove newlines and control characters to prevent log injection
            var sanitized = new string(input
                .Take(maxLength)
                .Where(c => !char.IsControl(c))
                .ToArray());

            return sanitized.Length < input.Length ? sanitized + "..." : sanitized;
        }
    }
}
