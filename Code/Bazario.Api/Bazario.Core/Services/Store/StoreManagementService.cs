using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO;
using Bazario.Core.Extensions;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;
using Bazario.Core.Enums;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Bazario.Core.DTO.Store;
using Bazario.Core.Helpers.Auth;
using Bazario.Core.Helpers.Store;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store management operations (CRUD)
    /// Handles store creation, updates, deletion, and status management
    /// </summary>
    public class StoreManagementService : IStoreManagementService
    {
        private readonly IStoreRepository _storeRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IStoreValidationService _validationService;
        private readonly IStoreManagementHelper _storeManagementHelper;
        private readonly ILogger<StoreManagementService> _logger;

        public StoreManagementService(
            IStoreRepository storeRepository,
            ISellerRepository sellerRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IStoreValidationService validationService,
            IStoreManagementHelper storeManagementHelper,
            ILogger<StoreManagementService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _sellerRepository = sellerRepository ?? throw new ArgumentNullException(nameof(sellerRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _storeManagementHelper = storeManagementHelper ?? throw new ArgumentNullException(nameof(storeManagementHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<StoreResponse> CreateStoreAsync(StoreAddRequest storeAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting store creation for seller: {SellerId}", storeAddRequest?.SellerId);

            try
            {
                // Validate input
                if (storeAddRequest == null)
                {
                    _logger.LogWarning("Store creation attempted with null request");
                    throw new ArgumentNullException(nameof(storeAddRequest));
                }

                // Validate seller exists
                var seller = await _sellerRepository.GetSellerByIdAsync(storeAddRequest.SellerId, cancellationToken);
                if (seller == null)
                {
                    _logger.LogWarning("Store creation failed: Seller not found. SellerId: {SellerId}", storeAddRequest.SellerId);
                    throw new InvalidOperationException($"Seller with ID {storeAddRequest.SellerId} not found");
                }

                // Validate store creation eligibility
                var validationResult = await _validationService.ValidateStoreCreationAsync(storeAddRequest.SellerId, storeAddRequest.Name!, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Store creation validation failed for seller: {SellerId}. Errors: {Errors}", 
                        storeAddRequest.SellerId, string.Join(", ", validationResult.ValidationErrors));
                    throw new InvalidOperationException($"Store creation validation failed: {string.Join(", ", validationResult.ValidationErrors)}");
                }

                // Create store entity
                var store = storeAddRequest.ToStore();
                store.StoreId = Guid.NewGuid();

                _logger.LogDebug("Creating store with ID: {StoreId}, Name: {StoreName}", store.StoreId, store.Name);

                // Save to repository
                var createdStore = await _storeRepository.AddStoreAsync(store, cancellationToken);

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
            _logger.LogInformation("Starting store update for store: {StoreId}", storeUpdateRequest?.StoreId);

            try
            {
                // Validate input
                if (storeUpdateRequest == null)
                {
                    _logger.LogWarning("Store update attempted with null request");
                    throw new ArgumentNullException(nameof(storeUpdateRequest));
                }

                // Get existing store
                var existingStore = await _storeRepository.GetStoreByIdAsync(storeUpdateRequest.StoreId, cancellationToken);
                if (existingStore == null)
                {
                    _logger.LogWarning("Store update failed: Store not found. StoreId: {StoreId}", storeUpdateRequest.StoreId);
                    throw new InvalidOperationException($"Store with ID {storeUpdateRequest.StoreId} not found");
                }

                // Check if name is changing and validate uniqueness for the seller
                if (!string.Equals(existingStore.Name, storeUpdateRequest.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var sellerStores = await _storeRepository.GetStoresBySellerIdAsync(existingStore.SellerId, cancellationToken);
                    if (sellerStores.Any(s => s.StoreId != existingStore.StoreId && 
                                            string.Equals(s.Name, storeUpdateRequest.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogWarning("Store update failed: Store name already exists for seller. Name: {StoreName}", storeUpdateRequest.Name);
                        throw new InvalidOperationException($"Store name '{storeUpdateRequest.Name}' already exists for this seller");
                    }
                }

                // Update store properties (only if provided)
                if (!string.IsNullOrEmpty(storeUpdateRequest.Name))
                {
                    existingStore.Name = storeUpdateRequest.Name;
                }
                
                if (storeUpdateRequest.Description != null)
                {
                    existingStore.Description = storeUpdateRequest.Description;
                }
                
                if (storeUpdateRequest.Category != null)
                {
                    existingStore.Category = storeUpdateRequest.Category;
                }
                
                if (storeUpdateRequest.Logo != null)
                {
                    existingStore.Logo = storeUpdateRequest.Logo;
                }
                
                // Update IsActive if provided
                if (storeUpdateRequest.IsActive.HasValue)
                {
                    existingStore.IsActive = storeUpdateRequest.IsActive.Value;
                }

                _logger.LogDebug("Updating store with ID: {StoreId}, Name: {StoreName}, IsActive: {IsActive}", 
                    existingStore.StoreId, existingStore.Name, existingStore.IsActive);

                // Save to repository
                var updatedStore = await _storeRepository.UpdateStoreAsync(existingStore, cancellationToken);

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
            _logger.LogInformation("Starting soft deletion for store: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                storeId, deletedBy, reason);

            try
            {
                // Validate inputs
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
                }

                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Store deletion failed: Store not found. StoreId: {StoreId}", storeId);
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Check if user can manage the store (owner or admin)
                var canManage = await _storeManagementHelper.CanUserManageStoreAsync(deletedBy, storeId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("Store deletion failed: User {UserId} is not authorized to delete store {StoreId}", deletedBy, storeId);
                    throw new UnauthorizedAccessException($"User {deletedBy} is not authorized to delete store {storeId}");
                }

                // For soft deletion, we don't need to check for active products or pending orders
                // because the data is preserved and can still be accessed
                _logger.LogDebug("Soft deletion allows store with active products and pending orders. StoreId: {StoreId}", storeId);

                _logger.LogDebug("Performing soft delete for store: {StoreId}", storeId);

                // Perform soft delete
                var result = await _storeRepository.SoftDeleteStoreAsync(storeId, deletedBy, reason, cancellationToken);

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
            _logger.LogWarning("HARD DELETE requested for store: {StoreId}, RequestedBy: {DeletedBy}, Reason: {Reason}", 
                storeId, deletedBy, reason);

            try
            {
                // Validate inputs
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

                // Check if user is an admin (hard delete requires admin privileges)
                var isAdmin = await _storeManagementHelper.IsUserAdminAsync(deletedBy, cancellationToken);
                if (!isAdmin)
                {
                    _logger.LogError("Unauthorized hard delete attempt by non-admin user: {UserId}", deletedBy);
                    throw new UnauthorizedAccessException("Only administrators can perform hard deletion");
                }

                _logger.LogWarning("Admin user {UserId} attempting hard delete of store {StoreId}", deletedBy, storeId);

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. StoreId: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                    storeId, deletedBy, reason);

                // Check if store exists (including soft-deleted)
                var store = await _storeRepository.GetStoreByIdIncludeDeletedAsync(storeId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Hard delete failed: Store not found. StoreId: {StoreId}", storeId);
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Check if store has any orders (hard delete NEVER allowed with orders)
                // Orders are critical business records that must be preserved permanently
                var storeOrderCount = await _orderRepository.GetOrderCountByStoreIdAsync(storeId, cancellationToken);
                if (storeOrderCount > 0)
                {
                    _logger.LogError("Hard delete BLOCKED: Store has existing orders. StoreId: {StoreId}, OrderCount: {OrderCount}", 
                        storeId, storeOrderCount);
                    throw new InvalidOperationException($"Cannot hard delete store with {storeOrderCount} existing orders. Orders are permanent business records and cannot be deleted.");
                }

                // Check if store has products and delete them first (due to Restrict delete behavior)
                var productCount = await _storeRepository.GetProductCountByStoreIdAsync(storeId, cancellationToken);
                if (productCount > 0)
                {
                    _logger.LogWarning("Store has {ProductCount} products that must be deleted first. StoreId: {StoreId}", 
                        productCount, storeId);
                    
                    // Get all products for this store
                    var products = await _productRepository.GetProductsByStoreIdAsync(storeId, cancellationToken);
                    
                    _logger.LogInformation("Hard deleting {ProductCount} products before store deletion. StoreId: {StoreId}", 
                        products.Count, storeId);
                    
                    // Hard delete all products first
                    foreach (var product in products)
                    {
                        try
                        {
                            var productDeleted = await _productRepository.HardDeleteProductAsync(product.ProductId, cancellationToken);
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
                var result = await _storeRepository.DeleteStoreByIdAsync(storeId, cancellationToken);

                if (result)
                {
                    _logger.LogCritical("HARD DELETE COMPLETED. Store permanently removed. StoreId: {StoreId}, DeletedBy: {DeletedBy}", 
                        storeId, deletedBy);
                }
                else
                {
                    _logger.LogError("Hard delete returned false. StoreId: {StoreId}", storeId);
                }

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
            _logger.LogInformation("Starting store restoration. StoreId: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);

            try
            {
                // Validate inputs
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                if (restoredBy == Guid.Empty)
                {
                    throw new ArgumentException("RestoredBy user ID cannot be empty", nameof(restoredBy));
                }

                // Check if store exists (including soft-deleted)
                var store = await _storeRepository.GetStoreByIdIncludeDeletedAsync(storeId, cancellationToken);
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

                // Check if user can manage the store (owner or admin)
                var canManage = await _storeManagementHelper.CanUserManageStoreAsync(restoredBy, storeId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("Store restoration failed: User {UserId} is not authorized to restore store {StoreId}", restoredBy, storeId);
                    throw new UnauthorizedAccessException($"User {restoredBy} is not authorized to restore store {storeId}");
                }

                _logger.LogDebug("Restoring store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                // Restore the store
                var result = await _storeRepository.RestoreStoreAsync(storeId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully restored store. StoreId: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);
                    
                    // Get the restored store
                    var restoredStore = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                    return restoredStore!.ToStoreResponse();
                }
                else
                {
                    _logger.LogError("Store restoration failed. StoreId: {StoreId}", storeId);
                    throw new InvalidOperationException($"Failed to restore store with ID {storeId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore store: {StoreId}, RestoredBy: {RestoredBy}", storeId, restoredBy);
                throw;
            }
        }

        public async Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, bool isActive, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating store status for store: {StoreId}, NewStatus: {IsActive}, Reason: {Reason}", 
                storeId, isActive, reason);

            try
            {
                // Validate inputs
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                // Get existing store
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Store status update failed: Store not found. StoreId: {StoreId}", storeId);
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Check if status is actually changing
                if (store.IsActive == isActive)
                {
                    _logger.LogDebug("Store status is already {Status} for store: {StoreId}", isActive ? "active" : "inactive", storeId);
                    return store.ToStoreResponse();
                }

                // Update store status
                store.IsActive = isActive;

                _logger.LogDebug("Updating store status. StoreId: {StoreId}, NewStatus: {IsActive}, Reason: {Reason}", 
                    storeId, isActive, reason);

                // Save to repository
                var updatedStore = await _storeRepository.UpdateStoreAsync(store, cancellationToken);

                _logger.LogInformation("Successfully updated store status. StoreId: {StoreId}, NewStatus: {IsActive}, Reason: {Reason}", 
                    storeId, isActive, reason);

                return updatedStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update store status for store: {StoreId}, NewStatus: {IsActive}", storeId, isActive);
                throw;
            }
        }
    }
}
