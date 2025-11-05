using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Extensions.Catalog;
using Bazario.Core.ServiceContracts.Authorization;
using Bazario.Core.ServiceContracts.Catalog.Product;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product management operations (CRUD)
    /// Handles product creation, updates, and deletion
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class ProductManagementService : IProductManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductValidationService _validationService;
        private readonly IAdminAuthorizationService _adminAuthService;
        private readonly ILogger<ProductManagementService> _logger;

        // Input validation constants
        private const int MINIMUM_DELETION_REASON_LENGTH = 10;
        private const int MAXIMUM_DELETION_REASON_LENGTH = 500;

        // Error message constants for consistency and potential localization
        private static class ErrorMessages
        {
            public const string ProductAddRequestNull = "Product creation attempted with null request";
            public const string StoreNotFound = "Store with ID {0} not found";
            public const string StoreInactive = "Cannot create product for inactive store {0}";
            public const string ConcurrencyConflictCreate = "The product was modified by another process. Please try again.";
            public const string DatabaseErrorCreate = "Failed to create product due to database error. Please check product data and try again.";
            public const string ProductUpdateRequestNull = "Product update attempted with null request";
            public const string ProductNotFound = "Product with ID {0} not found";
            public const string ConcurrencyConflictUpdate = "The product was modified by another user. Please refresh and try again.";
            public const string DatabaseErrorUpdate = "Failed to update product due to database error. Please check product data and try again.";
            public const string ProductIdEmpty = "Product ID cannot be empty";
            public const string DeletedByEmpty = "DeletedBy user ID cannot be empty";
            public const string RestoredByEmpty = "RestoredBy user ID cannot be empty";
            public const string ReasonRequired = "Reason is required for hard deletion";
            public const string ReasonTooShort = "Reason must be at least {0} characters long";
            public const string ReasonTooLong = "Reason cannot exceed {0} characters";
            public const string ProductCannotBeDeleted = "Product cannot be safely deleted due to active orders or dependencies";
            public const string ProductHasOrders = "Cannot hard delete product with {0} existing orders. Orders are permanent business records and cannot be deleted.";
            public const string ProductNotDeleted = "Product with ID {0} is not deleted and cannot be restored";
            public const string ProductRestorationFailed = "Failed to restore product with ID {0}";
            public const string ProductRestoredButNotRetrieved = "Product restoration succeeded but product could not be retrieved: {0}";
        }

        public ProductManagementService(
            IUnitOfWork unitOfWork,
            IProductValidationService validationService,
            IAdminAuthorizationService adminAuthService,
            ILogger<ProductManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _adminAuthService = adminAuthService ?? throw new ArgumentNullException(nameof(adminAuthService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductResponse> CreateProductAsync(ProductAddRequest productAddRequest, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting product creation for store: {StoreId}", productAddRequest?.StoreId);

            try
            {
                // Validate input
                if (productAddRequest == null)
                {
                    _logger.LogWarning(ErrorMessages.ProductAddRequestNull);
                    throw new ArgumentNullException(nameof(productAddRequest));
                }

                // Validate store exists
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(productAddRequest.StoreId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Product creation failed: Store not found. StoreId: {StoreId}", productAddRequest.StoreId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.StoreNotFound, productAddRequest.StoreId));
                }

                // Check for cancellation before continuing with validation
                cancellationToken.ThrowIfCancellationRequested();

                // Business Rule: Only active stores can create new products
                // Inactive stores are typically under maintenance or suspended
                if (!store.IsActive)
                {
                    _logger.LogWarning("Product creation failed: Store is inactive. StoreId: {StoreId}", productAddRequest.StoreId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.StoreInactive, productAddRequest.StoreId));
                }

                // Create product entity
                var product = productAddRequest.ToProduct();
                product.ProductId = Guid.NewGuid();

                _logger.LogDebug("Creating product with ID: {ProductId}, Name: {ProductName}, StoreId: {StoreId}", 
                    product.ProductId, product.Name, product.StoreId);

                // Save to repository
                var createdProduct = await _unitOfWork.Products.AddProductAsync(product, cancellationToken);

                // Transaction Handling: Persist changes with proper exception handling
                // DbUpdateConcurrencyException: Another process modified the product between read and write
                // DbUpdateException: Database constraint violation (e.g., duplicate key, FK violation)
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
                {
                    // Handle EF Core concurrency exception without direct reference
                    stopwatch.Stop();
                    _logger.LogError(ex, "Concurrency conflict creating product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                        product.ProductId, stopwatch.ElapsedMilliseconds);
                    throw new InvalidOperationException(ErrorMessages.ConcurrencyConflictCreate, ex);
                }
                catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
                {
                    // Handle EF Core database update exception without direct reference
                    stopwatch.Stop();
                    _logger.LogError(ex, "Database error creating product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                        product.ProductId, stopwatch.ElapsedMilliseconds);
                    throw new InvalidOperationException(ErrorMessages.DatabaseErrorCreate, ex);
                }

                stopwatch.Stop();
                _logger.LogInformation("Successfully created product. ProductId: {ProductId}, Name: {ProductName}, StoreId: {StoreId} (completed in {ElapsedMs}ms)",
                    createdProduct.ProductId, createdProduct.Name, createdProduct.StoreId, stopwatch.ElapsedMilliseconds);

                return createdProduct.ToProductResponse();
            }
            catch (Exception ex) when (ex is not (InvalidOperationException or ArgumentException or ArgumentNullException))
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error creating product for store: {StoreId} (failed after {ElapsedMs}ms)",
                    productAddRequest?.StoreId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting product update for product: {ProductId}", productUpdateRequest?.ProductId);

            try
            {
                // Validate input
                if (productUpdateRequest == null)
                {
                    _logger.LogWarning(ErrorMessages.ProductUpdateRequestNull);
                    throw new ArgumentNullException(nameof(productUpdateRequest));
                }

                // Get existing product
                var existingProduct = await _unitOfWork.Products.GetProductByIdAsync(productUpdateRequest.ProductId, cancellationToken);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product update failed: Product not found. ProductId: {ProductId}", productUpdateRequest.ProductId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotFound, productUpdateRequest.ProductId));
                }

                // Check for cancellation before processing update
                cancellationToken.ThrowIfCancellationRequested();

                // Convert DTO to entity for update
                var product = productUpdateRequest.ToProduct();

                _logger.LogDebug("Updating product with ID: {ProductId}, Name: {ProductName}", 
                    product.ProductId, product.Name);

                // Save to repository (repository handles null checks)
                var updatedProduct = await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                // Transaction Handling: Critical for preventing lost updates in concurrent scenarios
                // WARNING: Without RowVersion or optimistic locking, updates are susceptible to lost update problem
                // If two users update simultaneously, last write wins without detection
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
                {
                    // Handle EF Core concurrency exception - product was modified by another user
                    stopwatch.Stop();
                    _logger.LogWarning(ex, "Concurrency conflict updating product. ProductId: {ProductId}. Product was modified by another user (failed after {ElapsedMs}ms)",
                        productUpdateRequest.ProductId, stopwatch.ElapsedMilliseconds);
                    throw new InvalidOperationException(ErrorMessages.ConcurrencyConflictUpdate, ex);
                }
                catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
                {
                    // Handle EF Core database update exception
                    stopwatch.Stop();
                    _logger.LogError(ex, "Database error updating product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                        productUpdateRequest.ProductId, stopwatch.ElapsedMilliseconds);
                    throw new InvalidOperationException(ErrorMessages.DatabaseErrorUpdate, ex);
                }

                stopwatch.Stop();
                _logger.LogInformation("Successfully updated product. ProductId: {ProductId}, Name: {ProductName} (completed in {ElapsedMs}ms)",
                    updatedProduct.ProductId, updatedProduct.Name, stopwatch.ElapsedMilliseconds);

                return updatedProduct.ToProductResponse();
            }
            catch (Exception ex) when (ex is not (InvalidOperationException or ArgumentException or ArgumentNullException))
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error updating product: {ProductId} (failed after {ElapsedMs}ms)",
                    productUpdateRequest?.ProductId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting soft deletion for product: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                productId, deletedBy, reason);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.DeletedByEmpty, nameof(deletedBy));
                }

                // Validate product exists
                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product deletion failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotFound, productId));
                }

                // Business Rule: Soft deletion preserves all data and relationships
                // Products with active orders CAN be soft deleted because:
                // 1. Order history is preserved for auditing and analytics
                // 2. Customer order details remain accessible
                // 3. Data can be restored if deletion was accidental
                // 4. Referential integrity is maintained
                _logger.LogDebug("Soft deletion allows product with active orders. ProductId: {ProductId}", productId);

                _logger.LogDebug("Performing soft delete for product: {ProductId}, Name: {ProductName}", productId, product.Name);

                // Perform soft delete
                var result = await _unitOfWork.Products.SoftDeleteProductAsync(productId, deletedBy, reason, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                if (result)
                {
                    _logger.LogInformation("Successfully soft deleted product. ProductId: {ProductId}, Name: {ProductName}, DeletedBy: {DeletedBy} (completed in {ElapsedMs}ms)",
                        productId, product.Name, deletedBy, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Product soft deletion returned false. ProductId: {ProductId} (completed in {ElapsedMs}ms)",
                        productId, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to soft delete product: {ProductId}, DeletedBy: {DeletedBy} (failed after {ElapsedMs}ms)",
                    productId, deletedBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<bool> HardDeleteProductAsync(Guid productId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogWarning("HARD DELETE requested for product: {ProductId}, RequestedBy: {DeletedBy}, Reason: {Reason}",
                productId, deletedBy, reason);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.DeletedByEmpty, nameof(deletedBy));
                }

                // Validate and sanitize reason parameter
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException(ErrorMessages.ReasonRequired, nameof(reason));
                }

                reason = reason.Trim();

                if (reason.Length < MINIMUM_DELETION_REASON_LENGTH)
                {
                    throw new ArgumentException(string.Format(ErrorMessages.ReasonTooShort, MINIMUM_DELETION_REASON_LENGTH), nameof(reason));
                }

                if (reason.Length > MAXIMUM_DELETION_REASON_LENGTH)
                {
                    throw new ArgumentException(string.Format(ErrorMessages.ReasonTooLong, MAXIMUM_DELETION_REASON_LENGTH), nameof(reason));
                }

                // Validate admin privileges
                await _adminAuthService.ValidateAdminPrivilegesAsync(deletedBy, cancellationToken);

                // Check for cancellation before proceeding with deletion checks
                cancellationToken.ThrowIfCancellationRequested();

                // Check if product can be safely deleted
                if (!await _validationService.CanProductBeSafelyDeletedAsync(productId, cancellationToken))
                {
                    _logger.LogWarning("Product {ProductId} cannot be safely deleted - has active orders or dependencies", productId);
                    throw new InvalidOperationException(ErrorMessages.ProductCannotBeDeleted);
                }

                _logger.LogInformation("Admin user {UserId} performing hard delete of product {ProductId}", deletedBy, productId);

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. ProductId: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                    productId, deletedBy, reason);

                // Check if product exists (including soft-deleted)
                var product = await _unitOfWork.Products.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Hard delete failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotFound, productId));
                }

                // Check for cancellation before order validation
                cancellationToken.ThrowIfCancellationRequested();

                // Business Rule: Hard delete is NEVER allowed for products with orders
                // Orders are permanent business records that must be retained for:
                // 1. Legal compliance (tax records, receipts)
                // 2. Financial auditing and reconciliation
                // 3. Customer service and dispute resolution
                // 4. Business analytics and reporting
                // WARNING: This check is NOT atomic - race condition possible between check and delete
                // Consider adding database constraint for additional safety
                var productOrderCount = await _unitOfWork.Orders.GetOrderCountByProductIdAsync(productId, cancellationToken);
                if (productOrderCount > 0)
                {
                    _logger.LogError("Hard delete BLOCKED: Product has existing orders. ProductId: {ProductId}, OrderCount: {OrderCount}",
                        productId, productOrderCount);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductHasOrders, productOrderCount));
                }

                // Log product details before permanent deletion for audit
                _logger.LogCritical("Hard deleting product details - Name: {ProductName}, StoreId: {StoreId}, CreatedAt: {CreatedAt}, WasDeleted: {IsDeleted}", 
                    product.Name, product.StoreId, product.CreatedAt, product.IsDeleted);

                // Perform hard delete
                var result = await _unitOfWork.Products.HardDeleteProductAsync(productId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                if (result)
                {
                    _logger.LogCritical("HARD DELETE COMPLETED. Product permanently removed. ProductId: {ProductId}, DeletedBy: {DeletedBy} (completed in {ElapsedMs}ms)",
                        productId, deletedBy, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError("Hard delete returned false. ProductId: {ProductId} (completed in {ElapsedMs}ms)",
                        productId, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to hard delete product: {ProductId}, DeletedBy: {DeletedBy} (failed after {ElapsedMs}ms)",
                    productId, deletedBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<ProductResponse> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting product restoration. ProductId: {ProductId}, RestoredBy: {RestoredBy}", productId, restoredBy);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
                }

                if (restoredBy == Guid.Empty)
                {
                    throw new ArgumentException(ErrorMessages.RestoredByEmpty, nameof(restoredBy));
                }

                // Check if product exists (including soft-deleted)
                var product = await _unitOfWork.Products.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product restoration failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotFound, productId));
                }

                if (!product.IsDeleted)
                {
                    _logger.LogWarning("Product restoration failed: Product is not deleted. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotDeleted, productId));
                }

                // Check for cancellation before restoring
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Restoring product. ProductId: {ProductId}, Name: {ProductName}", productId, product.Name);

                // Restore the product
                var result = await _unitOfWork.Products.RestoreProductAsync(productId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (result)
                {
                    // Get the restored product with proper null check
                    var restoredProduct = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                    if (restoredProduct == null)
                    {
                        stopwatch.Stop();
                        _logger.LogError("Product restoration succeeded but product could not be retrieved. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                            productId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException(string.Format(ErrorMessages.ProductRestoredButNotRetrieved, productId));
                    }

                    stopwatch.Stop();
                    _logger.LogInformation("Successfully restored product. ProductId: {ProductId}, RestoredBy: {RestoredBy} (completed in {ElapsedMs}ms)",
                        productId, restoredBy, stopwatch.ElapsedMilliseconds);

                    return restoredProduct.ToProductResponse();
                }
                else
                {
                    stopwatch.Stop();
                    _logger.LogError("Product restoration failed. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                        productId, stopwatch.ElapsedMilliseconds);
                    throw new InvalidOperationException(string.Format(ErrorMessages.ProductRestorationFailed, productId));
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to restore product: {ProductId}, RestoredBy: {RestoredBy} (failed after {ElapsedMs}ms)",
                    productId, restoredBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

    }
}
