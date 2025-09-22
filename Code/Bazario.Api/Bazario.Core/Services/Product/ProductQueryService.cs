using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Product;
using Bazario.Core.Extensions;
using Bazario.Core.Models.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Product
{
    /// <summary>
    /// Service implementation for product query operations
    /// Handles product retrieval, searching, and filtering
    /// </summary>
    public class ProductQueryService : IProductQueryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductQueryService> _logger;

        public ProductQueryService(
            IProductRepository productRepository,
            ILogger<ProductQueryService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving product by ID: {ProductId}", productId);

            try
            {
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                
                if (product == null)
                {
                    _logger.LogDebug("Product not found: {ProductId}", productId);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved product: {ProductId}, Name: {ProductName}", 
                    product.ProductId, product.Name);

                return product.ToProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<List<ProductResponse>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products for store: {StoreId}", storeId);

            try
            {
                var products = await _productRepository.GetProductsByStoreIdAsync(storeId, cancellationToken);
                
                _logger.LogDebug("Successfully retrieved {Count} products for store: {StoreId}", 
                    products.Count, storeId);

                return products.Select(p => p.ToProductResponse()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<PagedResponse<ProductResponse>> SearchProductsAsync(ProductSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching products with criteria: {SearchTerm}, Category: {Category}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}", 
                searchCriteria.SearchTerm, searchCriteria.Category, searchCriteria.MinPrice, searchCriteria.MaxPrice);

            try
            {
                // Validate that at least one search criterion is provided
                if (string.IsNullOrWhiteSpace(searchCriteria.SearchTerm) && 
                    !searchCriteria.StoreId.HasValue &&
                    !searchCriteria.Category.HasValue && 
                    !searchCriteria.MinPrice.HasValue && 
                    !searchCriteria.MaxPrice.HasValue)
                {
                    throw new ArgumentException("At least one search criterion (SearchTerm, StoreId, Category, MinPrice, or MaxPrice) must be provided", nameof(searchCriteria));
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

                // Apply sorting (this becomes SQL ORDER BY)
                query = searchCriteria.SortBy?.ToLower() switch
                {
                    "name" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(p => p.Name) : 
                        query.OrderBy(p => p.Name),
                    "price" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(p => p.Price) : 
                        query.OrderBy(p => p.Price),
                    "createdat" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(p => p.CreatedAt) : 
                        query.OrderBy(p => p.CreatedAt),
                    "rating" => searchCriteria.SortDescending ? 
                        query.OrderByDescending(p => p.Reviews != null ? p.Reviews.Average(r => r.Rating) : 0) : 
                        query.OrderBy(p => p.Reviews != null ? p.Reviews.Average(r => r.Rating) : 0),
                    _ => query.OrderBy(p => p.Name)
                };

                // Get total count with SQL COUNT
                var totalCount = await _productRepository.GetProductsCountAsync(query, cancellationToken);

                // Apply pagination and execute query (this becomes SQL OFFSET/FETCH)
                var products = await _productRepository.GetProductsPagedAsync(query, searchCriteria.PageNumber, searchCriteria.PageSize, cancellationToken);

                var productResponses = products.Select(p => p.ToProductResponse()).ToList();

                var result = new PagedResponse<ProductResponse>
                {
                    Items = productResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully searched products. Found {TotalCount} products, returning page {PageNumber} with {ItemCount} items", 
                    totalCount, searchCriteria.PageNumber, productResponses.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search products with criteria: {SearchTerm}", searchCriteria.SearchTerm);
                throw;
            }
        }

        public async Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products with low stock (threshold: {Threshold})", threshold);

            try
            {
                var products = await _productRepository.GetLowStockProductsAsync(threshold, cancellationToken);
                
                _logger.LogDebug("Successfully found {Count} products with low stock", products.Count);

                return products.Select(p => p.ToProductResponse()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve low stock products with threshold: {Threshold}", threshold);
                throw;
            }
        }
    }
}
