using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Product;
using Bazario.Core.Extensions;
using Bazario.Core.ServiceContracts.Product;
using Bazario.Core.Helpers.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Product
{
    /// <summary>
    /// Service implementation for product management operations (CRUD)
    /// Handles product creation, updates, and deletion
    /// </summary>
    public class ProductManagementService : IProductManagementService
    {
        private readonly IProductRepository _productRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductValidationHelper _validationHelper;
        private readonly ILogger<ProductManagementService> _logger;

        public ProductManagementService(
            IProductRepository productRepository,
            IStoreRepository storeRepository,
            IOrderRepository orderRepository,
            IProductValidationHelper validationHelper,
            ILogger<ProductManagementService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductResponse> CreateProductAsync(ProductAddRequest productAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting product creation for store: {StoreId}", productAddRequest?.StoreId);

            try
            {
                // Validate input
                if (productAddRequest == null)
                {
                    _logger.LogWarning("Product creation attempted with null request");
                    throw new ArgumentNullException(nameof(productAddRequest));
                }

                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(productAddRequest.StoreId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Product creation failed: Store not found. StoreId: {StoreId}", productAddRequest.StoreId);
                    throw new InvalidOperationException($"Store with ID {productAddRequest.StoreId} not found");
                }

                // Validate store is active
                if (!store.IsActive)
                {
                    _logger.LogWarning("Product creation failed: Store is inactive. StoreId: {StoreId}", productAddRequest.StoreId);
                    throw new InvalidOperationException($"Cannot create product for inactive store {productAddRequest.StoreId}");
                }

                // Create product entity
                var product = productAddRequest.ToProduct();
                product.ProductId = Guid.NewGuid();

                _logger.LogDebug("Creating product with ID: {ProductId}, Name: {ProductName}, StoreId: {StoreId}", 
                    product.ProductId, product.Name, product.StoreId);

                // Save to repository
                var createdProduct = await _productRepository.AddProductAsync(product, cancellationToken);

                _logger.LogInformation("Successfully created product. ProductId: {ProductId}, Name: {ProductName}, StoreId: {StoreId}", 
                    createdProduct.ProductId, createdProduct.Name, createdProduct.StoreId);

                return createdProduct.ToProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create product for store: {StoreId}", productAddRequest?.StoreId);
                throw;
            }
        }

        public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting product update for product: {ProductId}", productUpdateRequest?.ProductId);

            try
            {
                // Validate input
                if (productUpdateRequest == null)
                {
                    _logger.LogWarning("Product update attempted with null request");
                    throw new ArgumentNullException(nameof(productUpdateRequest));
                }

                // Get existing product
                var existingProduct = await _productRepository.GetProductByIdAsync(productUpdateRequest.ProductId, cancellationToken);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product update failed: Product not found. ProductId: {ProductId}", productUpdateRequest.ProductId);
                    throw new InvalidOperationException($"Product with ID {productUpdateRequest.ProductId} not found");
                }

                // Convert DTO to entity for update
                var product = productUpdateRequest.ToProduct();

                _logger.LogDebug("Updating product with ID: {ProductId}, Name: {ProductName}", 
                    product.ProductId, product.Name);

                // Save to repository (repository handles null checks)
                var updatedProduct = await _productRepository.UpdateProductAsync(product, cancellationToken);

                _logger.LogInformation("Successfully updated product. ProductId: {ProductId}, Name: {ProductName}", 
                    updatedProduct.ProductId, updatedProduct.Name);

                return updatedProduct.ToProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product: {ProductId}", productUpdateRequest?.ProductId);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting soft deletion for product: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                productId, deletedBy, reason);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
                }

                // Validate product exists
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product deletion failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                // For soft deletion, we don't need to check for active orders
                // because the data is preserved and can still be accessed
                _logger.LogDebug("Soft deletion allows product with active orders. ProductId: {ProductId}", productId);

                _logger.LogDebug("Performing soft delete for product: {ProductId}, Name: {ProductName}", productId, product.Name);

                // Perform soft delete
                var result = await _productRepository.SoftDeleteProductAsync(productId, deletedBy, reason, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully soft deleted product. ProductId: {ProductId}, Name: {ProductName}, DeletedBy: {DeletedBy}", 
                        productId, product.Name, deletedBy);
                }
                else
                {
                    _logger.LogWarning("Product soft deletion returned false. ProductId: {ProductId}", productId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete product: {ProductId}, DeletedBy: {DeletedBy}", productId, deletedBy);
                throw;
            }
        }

        public async Task<bool> HardDeleteProductAsync(Guid productId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("HARD DELETE requested for product: {ProductId}, RequestedBy: {DeletedBy}, Reason: {Reason}", 
                productId, deletedBy, reason);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Reason is required for hard deletion", nameof(reason));
                }

                // Check if user has admin privileges
                if (!await _validationHelper.HasAdminPrivilegesAsync(deletedBy, cancellationToken))
                {
                    _logger.LogWarning("User {UserId} attempted hard delete of product {ProductId} without admin privileges", deletedBy, productId);
                    throw new UnauthorizedAccessException("Only administrators can perform hard deletion of products");
                }

                // Check if product can be safely deleted
                if (!await _validationHelper.CanProductBeSafelyDeletedAsync(productId, cancellationToken))
                {
                    _logger.LogWarning("Product {ProductId} cannot be safely deleted - has active orders or dependencies", productId);
                    throw new InvalidOperationException("Product cannot be safely deleted due to active orders or dependencies");
                }

                _logger.LogInformation("Admin user {UserId} performing hard delete of product {ProductId}", deletedBy, productId);

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. ProductId: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                    productId, deletedBy, reason);

                // Check if product exists (including soft-deleted)
                var product = await _productRepository.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Hard delete failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                // Check if product has any orders (hard delete NEVER allowed with orders)
                // Orders are critical business records that must be preserved permanently
                var productOrderCount = await _orderRepository.GetOrderCountByProductIdAsync(productId, cancellationToken);
                if (productOrderCount > 0)
                {
                    _logger.LogError("Hard delete BLOCKED: Product has existing orders. ProductId: {ProductId}, OrderCount: {OrderCount}", 
                        productId, productOrderCount);
                    throw new InvalidOperationException($"Cannot hard delete product with {productOrderCount} existing orders. Orders are permanent business records and cannot be deleted.");
                }

                // Log product details before permanent deletion for audit
                _logger.LogCritical("Hard deleting product details - Name: {ProductName}, StoreId: {StoreId}, CreatedAt: {CreatedAt}, WasDeleted: {IsDeleted}", 
                    product.Name, product.StoreId, product.CreatedAt, product.IsDeleted);

                // Perform hard delete
                var result = await _productRepository.HardDeleteProductAsync(productId, cancellationToken);

                if (result)
                {
                    _logger.LogCritical("HARD DELETE COMPLETED. Product permanently removed. ProductId: {ProductId}, DeletedBy: {DeletedBy}", 
                        productId, deletedBy);
                }
                else
                {
                    _logger.LogError("Hard delete returned false. ProductId: {ProductId}", productId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hard delete product: {ProductId}, DeletedBy: {DeletedBy}", productId, deletedBy);
                throw;
            }
        }

        public async Task<ProductResponse> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting product restoration. ProductId: {ProductId}, RestoredBy: {RestoredBy}", productId, restoredBy);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (restoredBy == Guid.Empty)
                {
                    throw new ArgumentException("RestoredBy user ID cannot be empty", nameof(restoredBy));
                }

                // Check if product exists (including soft-deleted)
                var product = await _productRepository.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product restoration failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                if (!product.IsDeleted)
                {
                    _logger.LogWarning("Product restoration failed: Product is not deleted. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} is not deleted and cannot be restored");
                }

                _logger.LogDebug("Restoring product. ProductId: {ProductId}, Name: {ProductName}", productId, product.Name);

                // Restore the product
                var result = await _productRepository.RestoreProductAsync(productId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully restored product. ProductId: {ProductId}, RestoredBy: {RestoredBy}", productId, restoredBy);
                    
                    // Get the restored product
                    var restoredProduct = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                    return restoredProduct!.ToProductResponse();
                }
                else
                {
                    _logger.LogError("Product restoration failed. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Failed to restore product with ID {productId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore product: {ProductId}, RestoredBy: {RestoredBy}", productId, restoredBy);
                throw;
            }
        }

    }
}
