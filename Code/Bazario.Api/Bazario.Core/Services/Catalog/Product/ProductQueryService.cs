using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Extensions.Catalog;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product query operations
    /// </summary>
    public class ProductQueryService : IProductQueryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductQueryService> _logger;
        private readonly int _maximumPageSize;
        private readonly int _maximumStockThreshold;

        // Default configuration values
        private const int MINIMUM_PAGE_NUMBER = 1;
        private const int MINIMUM_PAGE_SIZE = 1;
        private const int DEFAULT_MAXIMUM_PAGE_SIZE = 100;
        private const int DEFAULT_LOW_STOCK_THRESHOLD = 10;
        private const int DEFAULT_MAXIMUM_STOCK_THRESHOLD = 10000;

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
            IConfiguration configuration)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load configurable thresholds with defaults (aligned with ProductValidationService pattern)
            _maximumPageSize = GetConfigurationValue(configuration, ConfigurationKeys.MaximumPageSize, DEFAULT_MAXIMUM_PAGE_SIZE);
            _maximumStockThreshold = GetConfigurationValue(configuration, ConfigurationKeys.MaximumStockThreshold, DEFAULT_MAXIMUM_STOCK_THRESHOLD);
        }

        public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
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
                    stopwatch.Stop();
                    _logger.LogDebug("Product not found: {ProductId} (completed in {ElapsedMs}ms)", productId, stopwatch.ElapsedMilliseconds);
                    return null;
                }

                stopwatch.Stop();
                _logger.LogInformation("Successfully retrieved product: {ProductId}, Name: {ProductName} (completed in {ElapsedMs}ms)",
                    product.ProductId, product.Name, stopwatch.ElapsedMilliseconds);

                return product.ToProductResponse();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve product: {ProductId} (failed after {ElapsedMs}ms)", productId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<List<ProductResponse>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.StoreIdEmpty, nameof(storeId));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Retrieving products for store: {StoreId}", storeId);

            try
            {
                var products = await _productRepository.GetProductsByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation("Successfully retrieved {Count} products for store: {StoreId} (completed in {ElapsedMs}ms)",
                    products.Count, storeId, stopwatch.ElapsedMilliseconds);

                return products.Select(p => p.ToProductResponse()).ToList();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve products for store: {StoreId} (failed after {ElapsedMs}ms)", storeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<PagedResponse<ProductResponse>> SearchProductsAsync(ProductSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            if (searchCriteria == null)
            {
                throw new ArgumentNullException(nameof(searchCriteria), ErrorMessages.SearchCriteriaMissing);
            }

            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("Searching products with criteria: {SearchTerm}, Category: {Category}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
                searchCriteria.SearchTerm,
                searchCriteria.Category,
                searchCriteria.MinPrice?.ToString("N2", CultureInfo.InvariantCulture) ?? "N/A",
                searchCriteria.MaxPrice?.ToString("N2", CultureInfo.InvariantCulture) ?? "N/A");

            try
            {
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

                // Business Rule: At least one search filter required to prevent accidental full table scans
                if (string.IsNullOrWhiteSpace(searchCriteria.SearchTerm) &&
                    !searchCriteria.StoreId.HasValue &&
                    !searchCriteria.Category.HasValue &&
                    !searchCriteria.MinPrice.HasValue &&
                    !searchCriteria.MaxPrice.HasValue)
                {
                    throw new ArgumentException(ErrorMessages.NoSearchCriteriaProvided, nameof(searchCriteria));
                }

                // Start with IQueryable - stays as SQL
                var query = _productRepository.GetProductsQueryable();

                // Apply soft deletion filters (these become SQL WHERE clauses)
                if (searchCriteria.OnlyDeleted)
                {
                    // Need to ignore the global filter to get deleted products
                    query = _productRepository.GetProductsQueryableIgnoreFilters().Where(p => p.IsDeleted);
                }
                else if (searchCriteria.IncludeDeleted)
                {
                    // Need to ignore the global filter to include both active and deleted products
                    query = _productRepository.GetProductsQueryableIgnoreFilters();
                }
                // If neither OnlyDeleted nor IncludeDeleted, the global HasQueryFilter 
                // automatically applies !p.IsDeleted, so no additional filter needed

                // Apply filters (these become SQL WHERE clauses)
                if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
                {
                    query = query.Where(p => 
                        p.Name != null && p.Name.Contains(searchCriteria.SearchTerm) ||
                        p.Description != null && p.Description.Contains(searchCriteria.SearchTerm));
                }

                if (searchCriteria.StoreId.HasValue)
                {
                    query = query.Where(p => p.StoreId == searchCriteria.StoreId.Value);
                }

                if (searchCriteria.Category.HasValue)
                {
                    query = query.Where(p => p.Category == searchCriteria.Category.Value.ToString());
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
                var products = await _productRepository.GetProductsPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                var productResponses = products.Select(p => p.ToProductResponse()).ToList();

                var result = new PagedResponse<ProductResponse>
                {
                    Items = productResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "Product search completed in {ElapsedMs}ms: Found {TotalCount} products, Page {PageNumber}/{TotalPages} with {ItemCount} items",
                    stopwatch.ElapsedMilliseconds, totalCount, searchCriteria.PageNumber, result.TotalPages, productResponses.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Product search failed after {ElapsedMs}ms with criteria: {SearchTerm}",
                    stopwatch.ElapsedMilliseconds, searchCriteria.SearchTerm);
                throw;
            }
        }

        public async Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = DEFAULT_LOW_STOCK_THRESHOLD, CancellationToken cancellationToken = default)
        {
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

                stopwatch.Stop();
                _logger.LogInformation("Successfully found {Count} products with low stock (threshold: {Threshold}) (completed in {ElapsedMs}ms)",
                    products.Count, threshold, stopwatch.ElapsedMilliseconds);

                return products.Select(p => p.ToProductResponse()).ToList();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve low stock products with threshold: {Threshold} (failed after {ElapsedMs}ms)",
                    threshold, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        // Helper method to safely retrieve configuration values with defaults
        // Supports nullable types and uses culture-invariant parsing
        private static T GetConfigurationValue<T>(IConfiguration configuration, string key, T defaultValue)
        {
            if (configuration == null)
                return defaultValue;

            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            try
            {
                var targetType = typeof(T);
                // Handle nullable types by getting the underlying type
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // Use InvariantCulture for consistent parsing across different locales
                return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                // Log parsing failures for debugging (using Debug.WriteLine to avoid circular dependency)
                Debug.WriteLine($"Failed to parse configuration value '{key}' = '{value}': {ex.Message}");
                return defaultValue;
            }
        }
    }
}
