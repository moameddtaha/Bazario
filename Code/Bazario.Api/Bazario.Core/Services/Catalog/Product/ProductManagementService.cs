using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Extensions.Catalog;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.Helpers.Catalog;
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
        private readonly IConfigurationHelper _configHelper;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly IProductValidationHelper _productValidationHelper;
        private readonly int _minimumDeletionReasonLength;
        private readonly int _maximumDeletionReasonLength;

        // Default configuration values
        private const int DEFAULT_MINIMUM_DELETION_REASON_LENGTH = 10;
        private const int DEFAULT_MAXIMUM_DELETION_REASON_LENGTH = 500;

        // Configuration keys
        private static class ConfigurationKeys
        {
            public const string MinimumDeletionReasonLength = "Validation:MinimumDeletionReasonLength";
            public const string MaximumDeletionReasonLength = "Validation:MaximumDeletionReasonLength";
        }

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
            public const string ConcurrencyConflictDelete = "The product was modified by another process during deletion. Please try again.";
            public const string DatabaseErrorDelete = "Failed to delete product due to database error. Please try again.";
            public const string ConcurrencyConflictRestore = "The product was modified by another process during restoration. Please try again.";
            public const string DatabaseErrorRestore = "Failed to restore product due to database error. Please try again.";
        }

        public ProductManagementService(
            IUnitOfWork unitOfWork,
            IProductValidationService validationService,
            IAdminAuthorizationService adminAuthService,
            ILogger<ProductManagementService> logger,
            IConfigurationHelper configHelper,
            IConcurrencyHelper concurrencyHelper,
            IProductValidationHelper productValidationHelper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _adminAuthService = adminAuthService ?? throw new ArgumentNullException(nameof(adminAuthService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configHelper = configHelper ?? throw new ArgumentNullException(nameof(configHelper));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _productValidationHelper = productValidationHelper ?? throw new ArgumentNullException(nameof(productValidationHelper));

            // Load configurable thresholds with defaults (aligned with ProductValidationService pattern)
            _minimumDeletionReasonLength = _configHelper.GetValue(ConfigurationKeys.MinimumDeletionReasonLength, DEFAULT_MINIMUM_DELETION_REASON_LENGTH);
            _maximumDeletionReasonLength = _configHelper.GetValue(ConfigurationKeys.MaximumDeletionReasonLength, DEFAULT_MAXIMUM_DELETION_REASON_LENGTH);
        }

        public async Task<ProductResponse> CreateProductAsync(ProductAddRequest productAddRequest, CancellationToken cancellationToken = default)
        {
            // Validate input first (before starting stopwatch)
            if (productAddRequest == null)
            {
                throw new ArgumentNullException(nameof(productAddRequest));
            }

            // Perform comprehensive business validation
            ValidateProductAddRequest(productAddRequest);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting product creation for store: {StoreId}", productAddRequest.StoreId);

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

                // Begin explicit transaction to ensure atomicity
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Create product entity
                    var product = productAddRequest.ToProduct();
                    product.ProductId = Guid.NewGuid();

                    _logger.LogDebug("Creating product with ID: {ProductId}, Name: {ProductName}, StoreId: {StoreId}",
                        product.ProductId, product.Name, product.StoreId);

                    // Save to repository
                    var createdProduct = await _unitOfWork.Products.AddProductAsync(product, cancellationToken);

                    // Transaction Handling: Persist changes with proper exception handling
                    // DbUpdateException: Database constraint violation (e.g., duplicate key, FK violation)
                    // Note: Create operations don't use retry logic because there's no concurrency conflict
                    // on new entities (the entity doesn't exist yet, so no RowVersion to check)
                    try
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    }
                    catch (Exception ex) when (_productValidationHelper.IsDatabaseUpdateException(ex))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        _logger.LogError(ex, "Database error creating product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                            product.ProductId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException(ErrorMessages.DatabaseErrorCreate, ex);
                    }

                    _logger.LogInformation("Successfully created product. ProductId: {ProductId}, Name: {ProductName}, StoreId: {StoreId} (completed in {ElapsedMs}ms)",
                        createdProduct.ProductId, _productValidationHelper.GetProductNameForLogging(createdProduct.Name), createdProduct.StoreId, stopwatch.ElapsedMilliseconds);

                    return createdProduct.ToProductResponse()
                        ?? throw new InvalidOperationException("Failed to convert created product to response");
                }
                catch
                {
                    // Rollback transaction on any error
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating product for store: {StoreId} (failed after {ElapsedMs}ms)",
                    productAddRequest?.StoreId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, CancellationToken cancellationToken = default)
        {
            // Validate input first (before starting stopwatch)
            if (productUpdateRequest == null)
            {
                throw new ArgumentNullException(nameof(productUpdateRequest));
            }

            // Perform comprehensive business validation
            ValidateProductUpdateRequest(productUpdateRequest);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting product update for product: {ProductId}", productUpdateRequest.ProductId);

                // Wrap the entire update operation in retry logic
                // This ensures that on each retry, we re-fetch the product with the latest RowVersion
                var updatedProduct = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Begin explicit transaction to ensure atomicity
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Get existing product (this gets the latest RowVersion from the database)
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
                        var updated = await _unitOfWork.Products.UpdateProductAsync(product, cancellationToken);

                        // Transaction Handling: Critical for preventing lost updates in concurrent scenarios
                        // NOTE: RowVersion property is now included for optimistic concurrency control
                        // EF Core will automatically check RowVersion and throw DbUpdateConcurrencyException if mismatch
                        try
                        {
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                            return updated; // Success
                        }
                        catch (Exception ex) when (_productValidationHelper.IsDatabaseUpdateException(ex))
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            // Handle EF Core database update exception
                            _logger.LogError(ex, "Database error updating product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                                productUpdateRequest.ProductId, stopwatch.ElapsedMilliseconds);
                            throw new InvalidOperationException(ErrorMessages.DatabaseErrorUpdate, ex);
                        }
                    }
                    catch
                    {
                        // Rollback transaction on any error
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "UpdateProduct", cancellationToken);

                _logger.LogInformation("Successfully updated product. ProductId: {ProductId}, Name: {ProductName} (completed in {ElapsedMs}ms)",
                    updatedProduct.ProductId, _productValidationHelper.GetProductNameForLogging(updatedProduct.Name), stopwatch.ElapsedMilliseconds);

                return updatedProduct.ToProductResponse()
                    ?? throw new InvalidOperationException("Failed to convert updated product to response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating product: {ProductId} (failed after {ElapsedMs}ms)",
                    productUpdateRequest?.ProductId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<bool> DeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            // Validate inputs first
            if (productId == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
            }

            if (deletedBy == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.DeletedByEmpty, nameof(deletedBy));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting soft deletion for product: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                productId, deletedBy, _productValidationHelper.SanitizeForLogging(reason));

            // Perform safety validation check BEFORE deletion (outside retry - validation doesn't need retrying)
            // Note: This is informational only for soft deletes - we log warnings but don't block deletion
            // Soft deletion is safe because it preserves all data and relationships
            try
            {
                var canSafelyDelete = await _validationService.CanProductBeSafelyDeletedAsync(productId, cancellationToken);
                if (!canSafelyDelete)
                {
                    _logger.LogWarning("Product {ProductId} has active orders, reservations, or reviews. " +
                        "Proceeding with soft deletion (data will be preserved for audit trail).", productId);
                }
            }
            catch (Exception ex)
            {
                // Don't block deletion if validation check fails - just log the error
                _logger.LogWarning(ex, "Failed to perform safety validation check for product {ProductId}. " +
                    "Proceeding with soft deletion.", productId);
            }

            try
            {
                // Wrap the entire delete operation in retry logic
                // This ensures that on each retry, we re-fetch the product with the latest RowVersion
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Begin explicit transaction to ensure atomicity
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Validate product exists (including soft-deleted products to check double-deletion)
                        var product = await _unitOfWork.Products.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);
                        if (product == null)
                        {
                            _logger.LogWarning("Product deletion failed: Product not found. ProductId: {ProductId}", productId);
                            throw new InvalidOperationException(string.Format(ErrorMessages.ProductNotFound, productId));
                        }

                        // Business Rule: Prevent double-deletion to preserve audit trail
                        if (product.IsDeleted)
                        {
                            _logger.LogWarning("Product is already deleted. ProductId: {ProductId}, DeletedAt: {DeletedAt}, DeletedBy: {DeletedBy}",
                                productId, product.DeletedAt, product.DeletedBy);
                            throw new InvalidOperationException($"Product {productId} is already deleted");
                        }

                        // Check for cancellation before continuing
                        cancellationToken.ThrowIfCancellationRequested();

                        // Business Rule: Soft deletion preserves all data and relationships
                        // Products with active orders CAN be soft deleted because:
                        // 1. Order history is preserved for auditing and analytics
                        // 2. Customer order details remain accessible
                        // 3. Data can be restored if deletion was accidental
                        // 4. Referential integrity is maintained
                        _logger.LogDebug("Soft deletion allows product with active orders. ProductId: {ProductId}", productId);

                        _logger.LogDebug("Performing soft delete for product: {ProductId}, Name: {ProductName}",
                            productId, _productValidationHelper.GetProductNameForLogging(product.Name));

                        // Perform soft delete
                        var deleteResult = await _unitOfWork.Products.SoftDeleteProductAsync(productId, deletedBy, reason, cancellationToken);

                        // Transaction Handling: Persist changes with proper exception handling
                        try
                        {
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                            return deleteResult; // Success
                        }
                        catch (Exception ex) when (_productValidationHelper.IsDatabaseUpdateException(ex))
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogError(ex, "Database error deleting product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                                productId, stopwatch.ElapsedMilliseconds);
                            throw new InvalidOperationException(ErrorMessages.DatabaseErrorDelete, ex);
                        }
                    }
                    catch
                    {
                        // Rollback transaction on any error
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "DeleteProduct", cancellationToken);

                if (result)
                {
                    // Re-fetch to get the product name for logging
                    var deletedProduct = await _unitOfWork.Products.GetProductByIdIncludeDeletedAsync(productId, cancellationToken);

                    if (deletedProduct != null)
                    {
                        _logger.LogInformation("Successfully soft deleted product. ProductId: {ProductId}, Name: {ProductName}, DeletedBy: {DeletedBy} (completed in {ElapsedMs}ms)",
                            productId, _productValidationHelper.GetProductNameForLogging(deletedProduct.Name), deletedBy, stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        // Edge case: deletion succeeded but product couldn't be retrieved for logging
                        _logger.LogInformation("Successfully soft deleted product. ProductId: {ProductId}, DeletedBy: {DeletedBy} (completed in {ElapsedMs}ms) - Note: Product could not be retrieved for name logging",
                            productId, deletedBy, stopwatch.ElapsedMilliseconds);
                    }
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
                _logger.LogError(ex, "Failed to soft delete product: {ProductId}, DeletedBy: {DeletedBy} (failed after {ElapsedMs}ms)",
                    productId, deletedBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<bool> HardDeleteProductAsync(Guid productId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            // Validate inputs first
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

            if (reason.Length < _minimumDeletionReasonLength)
            {
                throw new ArgumentException(string.Format(ErrorMessages.ReasonTooShort, _minimumDeletionReasonLength), nameof(reason));
            }

            if (reason.Length > _maximumDeletionReasonLength)
            {
                throw new ArgumentException(string.Format(ErrorMessages.ReasonTooLong, _maximumDeletionReasonLength), nameof(reason));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogWarning("HARD DELETE requested for product: {ProductId}, RequestedBy: {DeletedBy}, Reason: {Reason}",
                productId, deletedBy, _productValidationHelper.SanitizeForLogging(reason));

            try
            {
                // Validate admin privileges
                await _adminAuthService.ValidateAdminPrivilegesAsync(deletedBy, cancellationToken);

                // Check for cancellation before proceeding with deletion checks
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Admin user {UserId} performing hard delete of product {ProductId}", deletedBy, productId);

            // Wrap the entire hard delete transaction in retry logic
            // This ensures that on each retry, we re-fetch the product with the latest RowVersion
            // and start a fresh transaction
            var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
            {
                // Begin explicit transaction to ensure atomicity
                // This prevents race conditions between validation and deletion
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
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
                    // NOTE: Now performed within transaction to prevent race conditions
                    var productOrderCount = await _unitOfWork.Orders.GetOrderCountByProductIdAsync(productId, cancellationToken);
                    if (productOrderCount > 0)
                    {
                        _logger.LogWarning("Hard delete BLOCKED: Product has existing orders. ProductId: {ProductId}, OrderCount: {OrderCount}",
                            productId, productOrderCount);
                        throw new InvalidOperationException(string.Format(ErrorMessages.ProductHasOrders, productOrderCount));
                    }

                    // Log product details before permanent deletion for audit
                    _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. ProductId: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                        productId, deletedBy, _productValidationHelper.SanitizeForLogging(reason));
                    _logger.LogCritical("Hard deleting product details - Name: {ProductName}, StoreId: {StoreId}, CreatedAt: {CreatedAt}, WasDeleted: {IsDeleted}",
                        _productValidationHelper.GetProductNameForLogging(product.Name), product.StoreId, product.CreatedAt, product.IsDeleted);

                    // Perform hard delete
                    var deleteResult = await _unitOfWork.Products.HardDeleteProductAsync(productId, cancellationToken);

                    // Check if hard delete operation succeeded before persisting
                    if (!deleteResult)
                    {
                        _logger.LogError("Hard delete returned false. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                            productId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException($"Hard delete failed for product {productId}");
                    }

                    // Transaction Handling: Persist changes with proper exception handling
                    try
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);
                        return deleteResult;
                    }
                    catch (Exception ex) when (_productValidationHelper.IsDatabaseUpdateException(ex))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        _logger.LogError(ex, "Database error during hard delete. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                            productId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException(ErrorMessages.DatabaseErrorDelete, ex);
                    }
                }
                catch
                {
                    // Rollback transaction on any error
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }, "HardDeleteProduct", cancellationToken);

            _logger.LogCritical("HARD DELETE COMPLETED. Product permanently removed. ProductId: {ProductId}, DeletedBy: {DeletedBy} (completed in {ElapsedMs}ms)",
                productId, deletedBy, stopwatch.ElapsedMilliseconds);

            return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hard delete product: {ProductId}, DeletedBy: {DeletedBy} (failed after {ElapsedMs}ms)",
                    productId, deletedBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<ProductResponse> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            // Validate inputs first
            if (productId == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.ProductIdEmpty, nameof(productId));
            }

            if (restoredBy == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.RestoredByEmpty, nameof(restoredBy));
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting product restoration. ProductId: {ProductId}, RestoredBy: {RestoredBy}", productId, restoredBy);


            try
            {
                // Wrap the entire restore operation in retry logic
                // This ensures that on each retry, we re-fetch the product with the latest RowVersion
                var restoredProduct = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Begin explicit transaction to ensure atomicity
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
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

                        _logger.LogDebug("Restoring product. ProductId: {ProductId}, Name: {ProductName}",
                            productId, _productValidationHelper.GetProductNameForLogging(product.Name));

                        // Restore the product
                        var result = await _unitOfWork.Products.RestoreProductAsync(productId, restoredBy, cancellationToken);

                        // Validate restoration succeeded BEFORE committing transaction
                        if (!result)
                        {
                            _logger.LogError("Product restoration failed. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                                productId, stopwatch.ElapsedMilliseconds);
                            throw new InvalidOperationException(string.Format(ErrorMessages.ProductRestorationFailed, productId));
                        }

                        // Verify the restored product can be retrieved BEFORE committing transaction
                        var restored = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                        if (restored == null)
                        {
                            _logger.LogError("Product restoration succeeded but product could not be retrieved. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                                productId, stopwatch.ElapsedMilliseconds);
                            throw new InvalidOperationException(string.Format(ErrorMessages.ProductRestoredButNotRetrieved, productId));
                        }

                        // Transaction Handling: Persist changes with proper exception handling
                        // All validations passed, now commit the transaction
                        try
                        {
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                            return restored;
                        }
                        catch (Exception ex) when (_productValidationHelper.IsDatabaseUpdateException(ex))
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            _logger.LogError(ex, "Database error restoring product. ProductId: {ProductId} (failed after {ElapsedMs}ms)",
                                productId, stopwatch.ElapsedMilliseconds);
                            throw new InvalidOperationException(ErrorMessages.DatabaseErrorRestore, ex);
                        }
                    }
                    catch
                    {
                        // Rollback transaction on any error
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "RestoreProduct", cancellationToken);

                _logger.LogInformation("Successfully restored product. ProductId: {ProductId}, RestoredBy: {RestoredBy} (completed in {ElapsedMs}ms)",
                    productId, restoredBy, stopwatch.ElapsedMilliseconds);

                return restoredProduct.ToProductResponse()
                    ?? throw new InvalidOperationException("Failed to convert restored product to response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore product: {ProductId}, RestoredBy: {RestoredBy} (failed after {ElapsedMs}ms)",
                    productId, restoredBy, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        // ========== Private Helper Methods ==========

        // Validation constants
        private const decimal MaxReasonablePrice = 1_000_000m;
        private const int MaxReasonableStock = 1_000_000;

        /// <summary>
        /// Validates ProductUpdateRequest for business rules beyond data annotations
        /// </summary>
        private void ValidateProductUpdateRequest(ProductUpdateRequest request)
        {
            var errors = new List<string>();

            // Validate Category enum value is defined (UpdateRequest has nullable Category)
            if (request.Category != null && !Enum.IsDefined(typeof(Category), request.Category))
            {
                errors.Add($"Invalid category value: {request.Category}");
            }

            // Perform common validation for both Add and Update requests
            ValidateCommonProductFields(
                request.Image,
                request.Price,
                request.StockQuantity,
                request.Name,
                errors);

            // If there are validation errors, throw aggregate exception
            if (errors.Count > 0)
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("ProductUpdateRequest validation failed: {Errors}", errorMessage);
                throw new ArgumentException($"Product validation failed: {errorMessage}");
            }
        }

        /// <summary>
        /// Validates ProductAddRequest for business rules beyond data annotations
        /// </summary>
        private void ValidateProductAddRequest(ProductAddRequest request)
        {
            var errors = new List<string>();

            // Validate Category enum value is defined (AddRequest has non-nullable Category)
            if (!Enum.IsDefined(typeof(Category), request.Category))
            {
                errors.Add($"Invalid category value: {request.Category}");
            }

            // Perform common validation for both Add and Update requests
            ValidateCommonProductFields(
                request.Image,
                request.Price,
                request.StockQuantity,
                request.Name,
                errors);

            // If there are validation errors, throw aggregate exception
            if (errors.Count > 0)
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("ProductAddRequest validation failed: {Errors}", errorMessage);
                throw new ArgumentException($"Product validation failed: {errorMessage}");
            }
        }

        /// <summary>
        /// Validates common product fields shared between Add and Update operations
        /// </summary>
        /// <param name="image">Product image URL</param>
        /// <param name="price">Product price (nullable for update requests)</param>
        /// <param name="stockQuantity">Product stock quantity (nullable for update requests)</param>
        /// <param name="name">Product name</param>
        /// <param name="errors">List to collect validation errors</param>
        private static void ValidateCommonProductFields(
            string? image,
            decimal? price,
            int? stockQuantity,
            string? name,
            List<string> errors)
        {
            // Validate Image URL format if provided
            if (!string.IsNullOrWhiteSpace(image))
            {
                if (!Uri.TryCreate(image, UriKind.Absolute, out var imageUri))
                {
                    errors.Add("Image must be a valid absolute URL");
                }
                else if (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps)
                {
                    errors.Add("Image URL must use HTTP or HTTPS protocol");
                }
            }

            // Validate reasonable price limits (prevent astronomical prices)
            if (price.HasValue && price.Value > MaxReasonablePrice)
            {
                errors.Add($"Price cannot exceed {MaxReasonablePrice:C}");
            }

            // Validate reasonable stock quantity limits
            if (stockQuantity.HasValue && stockQuantity.Value > MaxReasonableStock)
            {
                errors.Add($"Stock quantity cannot exceed {MaxReasonableStock:N0}");
            }

            // Validate Name is not just whitespace
            if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(name.Trim()))
            {
                errors.Add("Product name cannot be only whitespace");
            }
        }

    }
}
