using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;
using Bazario.Core.Enums;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Bazario.Core.DTO.Store;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Extensions.Store;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Bazario.Core.ServiceContracts.Order;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store management operations (CRUD)
    /// Handles store creation, updates, deletion, and status management
    /// </summary>
    public class StoreManagementService : IStoreManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreValidationService _validationService;
        private readonly IStoreAuthorizationService _authorizationService;
        private readonly IProductManagementService _productManagementService;
        private readonly IOrderManagementService _orderManagementService;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly ILogger<StoreManagementService> _logger;

        public StoreManagementService(
            IUnitOfWork unitOfWork,
            IStoreValidationService validationService,
            IStoreAuthorizationService authorizationService,
            IProductManagementService productManagementService,
            IOrderManagementService orderManagementService,
            IConcurrencyHelper concurrencyHelper,
            ILogger<StoreManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _productManagementService = productManagementService ?? throw new ArgumentNullException(nameof(productManagementService));
            _orderManagementService = orderManagementService ?? throw new ArgumentNullException(nameof(orderManagementService));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreResponse> CreateStoreAsync(StoreAddRequest storeAddRequest, CancellationToken cancellationToken = default)
        {
            // Validate input (outside retry)
            ArgumentNullException.ThrowIfNull(storeAddRequest);

            if (string.IsNullOrWhiteSpace(storeAddRequest.Name))
            {
                _logger.LogWarning("Store creation failed: Store name is required. SellerId: {SellerId}", storeAddRequest.SellerId);
                throw new ArgumentException("Store name is required", nameof(storeAddRequest));
            }

            _logger.LogInformation("Starting store creation for seller: {SellerId}", storeAddRequest.SellerId);

            // Validate seller exists (outside retry - one-time check)
            var seller = await _unitOfWork.Sellers.GetSellerByIdAsync(storeAddRequest.SellerId, cancellationToken);
            if (seller == null)
            {
                _logger.LogWarning("Store creation failed: Seller not found. SellerId: {SellerId}", storeAddRequest.SellerId);
                throw new InvalidOperationException($"Seller with ID {storeAddRequest.SellerId} not found");
            }

            // Validate store creation eligibility (outside retry)
            var validationResult = await _validationService.ValidateStoreCreationAsync(storeAddRequest.SellerId, storeAddRequest.Name, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Store creation validation failed for seller: {SellerId}. Errors: {Errors}",
                    storeAddRequest.SellerId, string.Join(", ", validationResult.ValidationErrors));
                throw new InvalidOperationException($"Store creation validation failed: {string.Join(", ", validationResult.ValidationErrors)}");
            }

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var createdStore = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Create store entity
                    var store = storeAddRequest.ToStore();
                    store.StoreId = Guid.NewGuid();

                    _logger.LogDebug("Creating store with ID: {StoreId}, Name: {StoreName}", store.StoreId, store.Name);

                    // Save to repository
                    var created = await _unitOfWork.Stores.AddStoreAsync(store, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return created;
                }, "CreateStore", cancellationToken);

                _logger.LogInformation("Successfully created store. StoreId: {StoreId}, Name: {StoreName}",
                    createdStore.StoreId, createdStore.Name);

                return createdStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create store for seller: {SellerId}", storeAddRequest?.SellerId);
                throw;
            }
        }

        public async Task<StoreResponse> UpdateStoreAsync(StoreUpdateRequest storeUpdateRequest, CancellationToken cancellationToken = default)
        {
            // Validate input (outside retry)
            ArgumentNullException.ThrowIfNull(storeUpdateRequest);

            _logger.LogInformation("Starting store update for store: {StoreId}", storeUpdateRequest.StoreId);

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var updatedStore = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Get existing store (INSIDE retry to get fresh RowVersion)
                    var existingStore = await _unitOfWork.Stores.GetStoreByIdAsync(storeUpdateRequest.StoreId, cancellationToken);
                    if (existingStore == null)
                    {
                        _logger.LogWarning("Store update failed: Store not found. StoreId: {StoreId}", storeUpdateRequest.StoreId);
                        throw new InvalidOperationException($"Store with ID {storeUpdateRequest.StoreId} not found");
                    }

                    // Check if name is changing and validate uniqueness for the seller (INSIDE retry - needs fresh data)
                    if (!string.IsNullOrEmpty(storeUpdateRequest.Name) &&
                        !string.Equals(existingStore.Name, storeUpdateRequest.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var sellerStores = await _unitOfWork.Stores.GetStoresBySellerIdAsync(existingStore.SellerId, cancellationToken);
                        if (sellerStores.Any(s => s.StoreId != existingStore.StoreId &&
                                                string.Equals(s.Name, storeUpdateRequest.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogWarning("Store update failed: Store name already exists for seller. Name: {StoreName}", storeUpdateRequest.Name);
                            throw new InvalidOperationException($"Store name '{storeUpdateRequest.Name}' already exists for this seller");
                        }
                    }

                    // Track if any changes were made
                    bool hasChanges = false;

                    // Update store properties (only if provided AND different from current value)
                    if (!string.IsNullOrEmpty(storeUpdateRequest.Name) &&
                        !string.Equals(existingStore.Name, storeUpdateRequest.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        existingStore.Name = storeUpdateRequest.Name;
                        hasChanges = true;
                        _logger.LogDebug("Store name changed to: {NewName}", storeUpdateRequest.Name);
                    }

                    if (storeUpdateRequest.Description != null &&
                        !string.Equals(existingStore.Description, storeUpdateRequest.Description, StringComparison.Ordinal))
                    {
                        existingStore.Description = storeUpdateRequest.Description;
                        hasChanges = true;
                        _logger.LogDebug("Store description changed");
                    }

                    if (storeUpdateRequest.Category != null &&
                        !string.Equals(existingStore.Category, storeUpdateRequest.Category, StringComparison.OrdinalIgnoreCase))
                    {
                        existingStore.Category = storeUpdateRequest.Category;
                        hasChanges = true;
                        _logger.LogDebug("Store category changed to: {NewCategory}", storeUpdateRequest.Category);
                    }

                    if (storeUpdateRequest.Logo != null &&
                        !string.Equals(existingStore.Logo, storeUpdateRequest.Logo, StringComparison.Ordinal))
                    {
                        existingStore.Logo = storeUpdateRequest.Logo;
                        hasChanges = true;
                        _logger.LogDebug("Store logo changed");
                    }

                    // Update RowVersion if provided
                    if (storeUpdateRequest.RowVersion != null)
                    {
                        existingStore.RowVersion = storeUpdateRequest.RowVersion;
                    }

                    // Note: IsActive is handled separately via UpdateStoreStatusAsync
                    if (storeUpdateRequest.IsActive.HasValue &&
                        existingStore.IsActive != storeUpdateRequest.IsActive.Value)
                    {
                        _logger.LogWarning("IsActive change requested in UpdateStore for store {StoreId}. Use UpdateStoreStatus instead", existingStore.StoreId);
                    }

                    // Only update if there are actual changes
                    if (!hasChanges)
                    {
                        _logger.LogInformation("No changes detected for store {StoreId}. Skipping database update", existingStore.StoreId);
                        return existingStore;
                    }

                    _logger.LogDebug("Updating store with ID: {StoreId}, Name: {StoreName}, IsActive: {IsActive}",
                        existingStore.StoreId, existingStore.Name, existingStore.IsActive);

                    // Save to repository
                    var updated = await _unitOfWork.Stores.UpdateStoreAsync(existingStore, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return updated;
                }, "UpdateStore", cancellationToken);

                _logger.LogInformation("Successfully updated store. StoreId: {StoreId}, Name: {StoreName}, IsActive: {IsActive}",
                    updatedStore.StoreId, updatedStore.Name, updatedStore.IsActive);

                return updatedStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update store: {StoreId}", storeUpdateRequest?.StoreId);
                throw;
            }
        }

        public async Task<bool> DeleteStoreAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            // Validate inputs (outside retry)
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            if (deletedBy == Guid.Empty)
            {
                throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
            }

            _logger.LogInformation("Starting soft deletion for store: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                storeId, deletedBy, reason);

            // Check authorization (outside retry - one-time check)
            var canManage = await _authorizationService.CanUserManageStoreAsync(deletedBy, storeId, cancellationToken);
            if (!canManage)
            {
                _logger.LogWarning("Store deletion failed: User {UserId} is not authorized to delete store {StoreId}", deletedBy, storeId);
                throw new UnauthorizedAccessException($"User {deletedBy} is not authorized to delete store {storeId}");
            }

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Validate store exists (INSIDE retry to get fresh data)
                    var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                    if (store == null)
                    {
                        _logger.LogWarning("Store deletion failed: Store not found. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Store with ID {storeId} not found");
                    }

                    // For soft deletion, we don't need to check for active products or pending orders
                    // because the data is preserved and can still be accessed
                    _logger.LogDebug("Soft deletion allows store with active products and pending orders. StoreId: {StoreId}", storeId);

                    _logger.LogDebug("Performing soft delete for store: {StoreId}", storeId);

                    // Perform soft delete
                    var deleteResult = await _unitOfWork.Stores.SoftDeleteStoreAsync(storeId, deletedBy, reason, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return deleteResult;
                }, "DeleteStore", cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully soft deleted store. StoreId: {StoreId}, DeletedBy: {DeletedBy}", storeId, deletedBy);
                }
                else
                {
                    _logger.LogWarning("Store soft deletion returned false. StoreId: {StoreId}", storeId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete store: {StoreId}, DeletedBy: {DeletedBy}", storeId, deletedBy);
                throw;
            }
        }

        public async Task<bool> HardDeleteStoreAsync(Guid storeId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            // Validate inputs (outside retry)
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            if (deletedBy == Guid.Empty)
            {
                throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Reason is required for hard deletion", nameof(reason));
            }

            _logger.LogWarning("HARD DELETE requested for store: {StoreId}, RequestedBy: {DeletedBy}, Reason: {Reason}",
                storeId, deletedBy, reason);

            // Check admin privileges (outside retry - one-time authorization check)
            var isAdmin = await _authorizationService.IsUserAdminAsync(deletedBy, cancellationToken);
            if (!isAdmin)
            {
                _logger.LogError("Unauthorized hard delete attempt by non-admin user: {UserId}", deletedBy);
                throw new UnauthorizedAccessException("Only administrators can perform hard deletion");
            }

            _logger.LogWarning("Admin user {UserId} attempting hard delete of store {StoreId}", deletedBy, storeId);

            _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. StoreId: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                storeId, deletedBy, reason);

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Check if store exists (INSIDE retry - including soft-deleted)
                    var store = await _unitOfWork.Stores.GetStoreByIdIncludeDeletedAsync(storeId, cancellationToken);
                    if (store == null)
                    {
                        _logger.LogWarning("Hard delete failed: Store not found. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Store with ID {storeId} not found");
                    }

                    // Begin transaction INSIDE retry for atomic hard delete (orders + products + store)
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // STEP 1: Check if store has orders and delete them first
                        var orders = await _unitOfWork.Orders.GetOrdersByStoreIdAsync(storeId, cancellationToken);
                        if (orders.Count > 0)
                        {
                            _logger.LogWarning("Store has {OrderCount} orders that must be deleted first. StoreId: {StoreId}",
                                orders.Count, storeId);

                            _logger.LogInformation("Hard deleting {OrderCount} orders before product/store deletion. StoreId: {StoreId}",
                                orders.Count, storeId);

                            // Hard delete all orders first (nested retry inside OrderManagementService)
                            foreach (var order in orders)
                            {
                                try
                                {
                                    var orderDeleted = await _orderManagementService.HardDeleteOrderAsync(
                                        order.OrderId,
                                        deletedBy,
                                        $"Cascade delete due to store hard deletion: {reason}",
                                        cancellationToken);

                                    if (orderDeleted)
                                    {
                                        _logger.LogDebug("Successfully hard deleted order: {OrderId}", order.OrderId);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to hard delete order: {OrderId}", order.OrderId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to hard delete order: {OrderId}", order.OrderId);
                                    throw new InvalidOperationException($"Failed to delete order {order.OrderId} before store deletion: {ex.Message}", ex);
                                }
                            }

                            _logger.LogInformation("Successfully hard deleted all {OrderCount} orders. StoreId: {StoreId}",
                                orders.Count, storeId);
                        }

                        // STEP 2: Check if store has products and delete them second (due to Restrict delete behavior)
                        var productCount = await _unitOfWork.Products.GetProductCountByStoreIdAsync(storeId, includeDeleted: true, cancellationToken);
                        if (productCount > 0)
                        {
                            _logger.LogWarning("Store has {ProductCount} products (including soft-deleted) that must be deleted. StoreId: {StoreId}",
                                productCount, storeId);

                            // Get all products for this store (including soft-deleted ones for complete cleanup)
                            var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId, includeDeleted: true, cancellationToken);

                            _logger.LogInformation("Hard deleting {ProductCount} products before store deletion. StoreId: {StoreId}",
                                products.Count, storeId);

                            // Hard delete all products using the service layer (nested retry inside ProductManagementService)
                            foreach (var product in products)
                            {
                                try
                                {
                                    var productDeleted = await _productManagementService.HardDeleteProductAsync(
                                        product.ProductId,
                                        deletedBy,
                                        $"Cascade delete due to store hard deletion: {reason}",
                                        cancellationToken);

                                    if (productDeleted)
                                    {
                                        _logger.LogDebug("Successfully hard deleted product: {ProductId}, Name: {ProductName}",
                                            product.ProductId, product.Name);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to hard delete product: {ProductId}, Name: {ProductName}",
                                            product.ProductId, product.Name);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to hard delete product: {ProductId}, Name: {ProductName}",
                                        product.ProductId, product.Name);
                                    throw new InvalidOperationException($"Failed to delete product {product.ProductId} ({product.Name}) before store deletion: {ex.Message}", ex);
                                }
                            }

                            _logger.LogInformation("Successfully hard deleted all {ProductCount} products. Proceeding with store deletion. StoreId: {StoreId}",
                                products.Count, storeId);
                        }

                        // Log store details before permanent deletion for audit
                        _logger.LogCritical("Hard deleting store details - Name: {StoreName}, SellerId: {SellerId}, CreatedAt: {CreatedAt}, WasDeleted: {IsDeleted}",
                            store.Name, store.SellerId, store.CreatedAt, store.IsDeleted);

                        // Perform hard delete
                        var deleteResult = await _unitOfWork.Stores.DeleteStoreByIdAsync(storeId, cancellationToken);

                        if (deleteResult)
                        {
                            // Commit transaction
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                            _logger.LogCritical("HARD DELETE COMPLETED. Store permanently removed. StoreId: {StoreId}, DeletedBy: {DeletedBy}",
                                storeId, deletedBy);
                        }
                        else
                        {
                            _logger.LogError("Hard delete returned false. StoreId: {StoreId}", storeId);
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        }

                        return deleteResult;
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "HardDeleteStore", cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hard delete store: {StoreId}, DeletedBy: {DeletedBy}", storeId, deletedBy);
                throw;
            }
        }

        public async Task<StoreResponse> RestoreStoreAsync(Guid storeId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            // Validate inputs (outside retry)
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            if (restoredBy == Guid.Empty)
            {
                throw new ArgumentException("RestoredBy user ID cannot be empty", nameof(restoredBy));
            }

            _logger.LogInformation("Starting store restoration. StoreId: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);

            // Check authorization (outside retry - one-time check)
            var canManage = await _authorizationService.CanUserManageStoreAsync(restoredBy, storeId, cancellationToken);
            if (!canManage)
            {
                _logger.LogWarning("Store restoration failed: User {UserId} is not authorized to restore store {StoreId}", restoredBy, storeId);
                throw new UnauthorizedAccessException($"User {restoredBy} is not authorized to restore store {storeId}");
            }

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var restoredStore = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Check if store exists (INSIDE retry - including soft-deleted, to get fresh RowVersion)
                    var store = await _unitOfWork.Stores.GetStoreByIdIncludeDeletedAsync(storeId, cancellationToken);
                    if (store == null)
                    {
                        _logger.LogWarning("Store restoration failed: Store not found. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Store with ID {storeId} not found");
                    }

                    if (!store.IsDeleted)
                    {
                        _logger.LogWarning("Store restoration failed: Store is not deleted. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Store with ID {storeId} is not deleted and cannot be restored");
                    }

                    _logger.LogDebug("Restoring store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                    // Restore the store
                    var result = await _unitOfWork.Stores.RestoreStoreAsync(storeId, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (!result)
                    {
                        _logger.LogError("Store restoration failed. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Failed to restore store with ID {storeId}");
                    }

                    // Get the restored store with fresh data
                    var restored = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                    return restored!;
                }, "RestoreStore", cancellationToken);

                _logger.LogInformation("Successfully restored store. StoreId: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);

                return restoredStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore store: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);
                throw;
            }
        }

        public async Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, Guid userId, bool isActive, string? reason = null, CancellationToken cancellationToken = default)
        {
            // Validate inputs (outside retry)
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            _logger.LogInformation("Updating store status for store: {StoreId}, User: {UserId}, NewStatus: {IsActive}, Reason: {Reason}",
                storeId, userId, isActive, reason);

            // Check authorization (outside retry - one-time check)
            var canManage = await _authorizationService.CanUserManageStoreAsync(userId, storeId, cancellationToken);
            if (!canManage)
            {
                _logger.LogWarning("Store status update failed: User {UserId} is not authorized to manage store {StoreId}", userId, storeId);
                throw new UnauthorizedAccessException($"User {userId} is not authorized to manage store {storeId}");
            }

            try
            {
                // Wrap entire operation in retry for optimistic concurrency control
                var updatedStore = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Get existing store (INSIDE retry to get fresh RowVersion)
                    var store = await _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                    if (store == null)
                    {
                        _logger.LogWarning("Store status update failed: Store not found. StoreId: {StoreId}", storeId);
                        throw new InvalidOperationException($"Store with ID {storeId} not found");
                    }

                    // Business validation: Cannot activate a deleted store (INSIDE retry - needs fresh data)
                    if (store.IsDeleted && isActive)
                    {
                        _logger.LogWarning("Cannot activate deleted store: {StoreId}. Please restore it first.", storeId);
                        throw new InvalidOperationException($"Cannot activate deleted store {storeId}. Please restore it first.");
                    }

                    // Check if status is actually changing
                    if (store.IsActive == isActive)
                    {
                        _logger.LogDebug("Store status is already {Status} for store: {StoreId}", isActive ? "active" : "inactive", storeId);
                        return store;
                    }

                    // Update store status and audit fields
                    store.IsActive = isActive;
                    store.UpdatedBy = userId;
                    store.UpdatedAt = DateTime.UtcNow;

                    _logger.LogDebug("Updating store status. StoreId: {StoreId}, NewStatus: {IsActive}, UpdatedBy: {UserId}, Reason: {Reason}",
                        storeId, isActive, userId, reason);

                    // Save to repository
                    var updated = await _unitOfWork.Stores.UpdateStoreAsync(store, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return updated;
                }, "UpdateStoreStatus", cancellationToken);

                _logger.LogInformation("Successfully updated store status. StoreId: {StoreId}, NewStatus: {IsActive}, UpdatedBy: {UserId}, Reason: {Reason}",
                    storeId, isActive, userId, reason);

                return updatedStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update store status for store: {StoreId}, User: {UserId}, NewStatus: {IsActive}", storeId, userId, isActive);
                throw;
            }
        }
    }
}
