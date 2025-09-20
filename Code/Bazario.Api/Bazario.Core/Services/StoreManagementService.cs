using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO;
using Bazario.Core.Extensions;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services
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
        private readonly ILogger<StoreManagementService> _logger;

        public StoreManagementService(
            IStoreRepository storeRepository,
            ISellerRepository sellerRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IStoreValidationService validationService,
            ILogger<StoreManagementService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _sellerRepository = sellerRepository ?? throw new ArgumentNullException(nameof(sellerRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
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

                // Update store properties
                existingStore.Name = storeUpdateRequest.Name;
                existingStore.Description = storeUpdateRequest.Description;
                existingStore.Category = storeUpdateRequest.Category;
                existingStore.Logo = storeUpdateRequest.Logo;

                _logger.LogDebug("Updating store with ID: {StoreId}, Name: {StoreName}", existingStore.StoreId, existingStore.Name);

                // Save to repository
                var updatedStore = await _storeRepository.UpdateStoreAsync(existingStore, cancellationToken);

                _logger.LogInformation("Successfully updated store. StoreId: {StoreId}, Name: {StoreName}", 
                    updatedStore.StoreId, updatedStore.Name);

                return updatedStore.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update store: {StoreId}", storeUpdateRequest?.StoreId);
                throw;
            }
        }

        public async Task<bool> DeleteStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting store deletion for store: {StoreId}", storeId);

            try
            {
                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Store deletion failed: Store not found. StoreId: {StoreId}", storeId);
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Check if store has active products
                var productCount = await _storeRepository.GetProductCountByStoreIdAsync(storeId, cancellationToken);
                if (productCount > 0)
                {
                    _logger.LogWarning("Store deletion failed: Store has active products. StoreId: {StoreId}, ProductCount: {ProductCount}", 
                        storeId, productCount);
                    throw new InvalidOperationException($"Cannot delete store with {productCount} active products. Please remove all products first.");
                }

                // Check if store has pending orders (basic check - could be enhanced)
                // Note: This is a simplified check - in reality we'd need to join through OrderItems to get store-related orders
                var allOrders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
                var pendingOrders = allOrders.Where(o => o.Status == Core.Enums.OrderStatus.Pending.ToString() || 
                                                        o.Status == Core.Enums.OrderStatus.Processing.ToString()).ToList();
                
                if (pendingOrders.Any())
                {
                    _logger.LogWarning("Store deletion failed: Store has pending orders. StoreId: {StoreId}, PendingOrderCount: {PendingOrderCount}", 
                        storeId, pendingOrders.Count);
                    throw new InvalidOperationException($"Cannot delete store with {pendingOrders.Count} pending orders. Please complete or cancel all pending orders first.");
                }

                _logger.LogDebug("Deleting store with ID: {StoreId}", storeId);

                // Delete store
                var result = await _storeRepository.DeleteStoreByIdAsync(storeId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully deleted store. StoreId: {StoreId}", storeId);
                }
                else
                {
                    _logger.LogWarning("Store deletion returned false. StoreId: {StoreId}", storeId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, bool isActive, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating store status for store: {StoreId}, NewStatus: {IsActive}, Reason: {Reason}", 
                storeId, isActive, reason);

            try
            {
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {storeId} not found");
                }

                // Note: The Store entity doesn't have an IsActive field in the current model
                // This would need to be added to the Store entity and database schema
                // For now, we'll just return the store as-is and log the status change request

                _logger.LogWarning("Store status update requested but Store entity doesn't have IsActive field. StoreId: {StoreId}", storeId);
                
                // TODO: Add IsActive field to Store entity and update this implementation
                // store.IsActive = isActive;
                // var updatedStore = await _storeRepository.UpdateStoreAsync(store, cancellationToken);

                _logger.LogInformation("Store status update logged for store: {StoreId}", storeId);
                return store.ToStoreResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update store status for store: {StoreId}", storeId);
                throw;
            }
        }
    }
}
