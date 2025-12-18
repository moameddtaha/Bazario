using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Composite service implementation for product operations
    /// Delegates to specialized services while providing a unified interface
    /// Follows Single Responsibility Principle by delegating to specialized services
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductManagementService _managementService;
        private readonly IProductQueryService _queryService;
        private readonly IProductAnalyticsService _analyticsService;
        private readonly IProductValidationService _validationService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductManagementService managementService,
            IProductQueryService queryService,
            IProductAnalyticsService analyticsService,
            IProductValidationService validationService,
            ILogger<ProductService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // IProductManagementService methods
        public async Task<ProductResponse> CreateProductAsync(ProductAddRequest productAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CreateProductAsync to ProductManagementService");
            return await _managementService.CreateProductAsync(productAddRequest, cancellationToken);
        }

        public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating UpdateProductAsync to ProductManagementService");
            return await _managementService.UpdateProductAsync(productUpdateRequest, cancellationToken);
        }

        public async Task<bool> DeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating DeleteProductAsync to ProductManagementService");
            return await _managementService.DeleteProductAsync(productId, deletedBy, reason, cancellationToken);
        }

        public async Task<bool> HardDeleteProductAsync(Guid productId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating HardDeleteProductAsync to ProductManagementService");
            return await _managementService.HardDeleteProductAsync(productId, deletedBy, reason, cancellationToken);
        }

        public async Task<ProductResponse> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating RestoreProductAsync to ProductManagementService");
            return await _managementService.RestoreProductAsync(productId, restoredBy, cancellationToken);
        }

        // IProductQueryService methods
        public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetProductByIdAsync to ProductQueryService");
            return await _queryService.GetProductByIdAsync(productId, cancellationToken);
        }

        public async Task<PagedResponse<ProductResponse>> GetProductsByStoreIdAsync(Guid storeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetProductsByStoreIdAsync to ProductQueryService");
            return await _queryService.GetProductsByStoreIdAsync(storeId, pageNumber, pageSize, cancellationToken);
        }

        public async Task<PagedResponse<ProductResponse>> SearchProductsAsync(ProductSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating SearchProductsAsync to ProductQueryService");
            return await _queryService.SearchProductsAsync(searchCriteria, cancellationToken);
        }

        public async Task<List<ProductResponse>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetLowStockProductsAsync to ProductQueryService");
            return await _queryService.GetLowStockProductsAsync(threshold, cancellationToken);
        }

        // IProductAnalyticsService methods
        public async Task<ProductAnalytics> GetProductAnalyticsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetProductAnalyticsAsync to ProductAnalyticsService");
            return await _analyticsService.GetProductAnalyticsAsync(productId, cancellationToken);
        }

        // IProductValidationService methods
        public async Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating ValidateForOrderAsync to ProductValidationService");
            return await _validationService.ValidateForOrderAsync(productId, quantity, cancellationToken);
        }

        public async Task<bool> CanProductBeSafelyDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CanProductBeSafelyDeletedAsync to ProductValidationService");
            return await _validationService.CanProductBeSafelyDeletedAsync(productId, cancellationToken);
        }

        public async Task<bool> HasProductActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating HasProductActiveOrdersAsync to ProductValidationService");
            return await _validationService.HasProductActiveOrdersAsync(productId, cancellationToken);
        }

        public async Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating HasProductPendingReservationsAsync to ProductValidationService");
            return await _validationService.HasProductPendingReservationsAsync(productId, cancellationToken);
        }

        public async Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating HasProductReviewsAsync to ProductValidationService");
            return await _validationService.HasProductReviewsAsync(productId, cancellationToken);
        }
    }
}
