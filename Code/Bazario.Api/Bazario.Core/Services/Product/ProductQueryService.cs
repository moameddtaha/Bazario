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
                var products = await _productRepository.SearchProductsAsync(searchCriteria, cancellationToken);
                
                var productResponses = products.Select(p => p.ToProductResponse()).ToList();

                _logger.LogDebug("Successfully found {Count} products matching search criteria", productResponses.Count);

                return new PagedResponse<ProductResponse>
                {
                    Items = productResponses,
                    TotalCount = productResponses.Count,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };
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
