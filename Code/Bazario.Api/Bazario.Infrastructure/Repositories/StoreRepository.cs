using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StoreRepository> _logger;

        public StoreRepository(ApplicationDbContext context, ILogger<StoreRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Store> AddStoreAsync(Store store, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new store: {StoreName}", store?.Name);
            
            try
            {
                // Validate input
                if (store == null)
                {
                    _logger.LogWarning("Attempted to add null store");
                    throw new ArgumentNullException(nameof(store));
                }

                _logger.LogDebug("Adding store to database context. StoreId: {StoreId}, Name: {StoreName}, SellerId: {SellerId}", 
                    store.StoreId, store.Name, store.SellerId);

                // Add store to context
                _context.Stores.Add(store);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added store. StoreId: {StoreId}, Name: {StoreName}", 
                    store.StoreId, store.Name);

                return store;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding store: {StoreName}", store?.Name);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating store: {StoreName}", store?.Name);
                throw new InvalidOperationException($"Unexpected error while creating store: {ex.Message}", ex);
            }
        }

        public async Task<Store> UpdateStoreAsync(Store store, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update store: {StoreId}", store?.StoreId);
            
            try
            {
                // Validate input
                if (store == null)
                {
                    _logger.LogWarning("Attempted to update null store");
                    throw new ArgumentNullException(nameof(store));
                }

                if (store.StoreId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update store with empty ID");
                    throw new ArgumentException("Store ID cannot be empty", nameof(store));
                }

                _logger.LogDebug("Checking if store exists in database. StoreId: {StoreId}", store.StoreId);

                // Check if store exists (use FindAsync for simple PK lookup)
                var existingStore = await _context.Stores.FindAsync(new object[] { store.StoreId }, cancellationToken);
                if (existingStore == null)
                {
                    _logger.LogWarning("Store not found for update. StoreId: {StoreId}", store.StoreId);
                    throw new InvalidOperationException($"Store with ID {store.StoreId} not found");
                }

                _logger.LogDebug("Updating store properties. StoreId: {StoreId}, Name: {Name}, Category: {Category}, IsActive: {IsActive}", 
                    store.StoreId, store.Name, store.Category, store.IsActive);

                // Update only specific properties (not foreign keys or primary key)
                existingStore.Name = store.Name;
                existingStore.Description = store.Description;
                existingStore.Category = store.Category;
                existingStore.Logo = store.Logo;
                existingStore.IsActive = store.IsActive;
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated store. StoreId: {StoreId}, Name: {Name}, Category: {Category}, IsActive: {IsActive}", 
                    store.StoreId, store.Name, store.Category, store.IsActive);

                return existingStore;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating store: {StoreId}", store?.StoreId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating store: {StoreId}", store?.StoreId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating store: {StoreId}", store?.StoreId);
                throw new InvalidOperationException($"Unexpected error while updating store with ID {store?.StoreId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Hard delete method called for store: {StoreId}. Consider using SoftDeleteStoreAsync instead.", storeId);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete store with empty ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Checking if store exists for hard deletion. StoreId: {StoreId}", storeId);

                // Use IgnoreQueryFilters to find even soft-deleted stores for hard delete
                var store = await _context.Stores
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);
                
                if (store == null)
                {
                    _logger.LogWarning("Store not found for hard deletion. StoreId: {StoreId}", storeId);
                    return false; // Store not found
                }

                _logger.LogDebug("Hard deleting store from database. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                // Hard delete the store (permanent removal)
                _context.Stores.Remove(store);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully hard deleted store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while hard deleting store: {StoreId}", storeId);
                throw new InvalidOperationException($"Unexpected error while hard deleting store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> SoftDeleteStoreAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting soft delete for store: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}", storeId, deletedBy, reason);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete store with empty ID");
                    return false;
                }

                if (deletedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete store without valid DeletedBy user ID");
                    return false;
                }

                _logger.LogDebug("Checking if store exists for soft deletion. StoreId: {StoreId}", storeId);

                // Find the store (should be active, not already deleted)
                var store = await _context.Stores
                .IgnoreQueryFilters() // This allows finding deleted stores
                .FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);
                
                if (store == null)
                {
                    _logger.LogWarning("Store not found for soft deletion. StoreId: {StoreId}", storeId);
                    return false;
                }

                if (store.IsDeleted)
                {
                    _logger.LogWarning("Store is already soft deleted. StoreId: {StoreId}", storeId);
                    return false;
                }

                _logger.LogDebug("Soft deleting store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                // Set soft delete properties
                store.IsDeleted = true;
                store.DeletedAt = DateTime.UtcNow;
                store.DeletedBy = deletedBy;
                store.DeletedReason = reason;

                // Update the store
                _context.Stores.Update(store);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully soft deleted store. StoreId: {StoreId}, Name: {StoreName}, DeletedBy: {DeletedBy}", 
                    storeId, store.Name, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while soft deleting store: {StoreId}", storeId);
                throw new InvalidOperationException($"Unexpected error while soft deleting store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting restore for soft deleted store: {StoreId}", storeId);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to restore store with empty ID");
                    return false;
                }

                _logger.LogDebug("Checking if soft deleted store exists for restore. StoreId: {StoreId}", storeId);

                // Find the store including soft deleted ones
                var store = await _context.Stores
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);
                
                if (store == null)
                {
                    _logger.LogWarning("Store not found for restore. StoreId: {StoreId}", storeId);
                    return false;
                }

                if (!store.IsDeleted)
                {
                    _logger.LogWarning("Store is not soft deleted, cannot restore. StoreId: {StoreId}", storeId);
                    return false;
                }

                _logger.LogDebug("Restoring soft deleted store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                // Clear soft delete properties
                store.IsDeleted = false;
                store.DeletedAt = null;
                store.DeletedBy = null;
                store.DeletedReason = null;

                // Update the store
                _context.Stores.Update(store);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully restored store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while restoring store: {StoreId}", storeId);
                throw new InvalidOperationException($"Unexpected error while restoring store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<Store?> GetStoreByIdIncludeDeletedAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve store with empty ID");
                    return null;
                }

                _logger.LogDebug("Retrieving store including soft deleted. StoreId: {StoreId}", storeId);

                // Query with navigation properties, ignoring soft delete filter
                var store = await _context.Stores
                    .IgnoreQueryFilters()
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);

                if (store != null)
                {
                    _logger.LogDebug("Successfully retrieved store including deleted. StoreId: {StoreId}, Name: {StoreName}, IsDeleted: {IsDeleted}", 
                        storeId, store.Name, store.IsDeleted);
                }
                else
                {
                    _logger.LogDebug("Store not found including deleted. StoreId: {StoreId}", storeId);
                }

                return store;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve store including deleted: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to retrieve store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve store with empty ID");
                    return null; // Invalid ID
                }

                // Find the store with navigation properties
                var store = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);

                return store;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to retrieve store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetAllStoresAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve all stores");
            
            try
            {
                _logger.LogDebug("Querying all stores with navigation properties");

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} stores", stores.Count);

                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all stores");
                throw new InvalidOperationException($"Failed to retrieve stores: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve stores for seller: {SellerId}", sellerId);
            
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve stores with empty seller ID");
                    return new List<Store>(); // Invalid ID, return empty list
                }

                _logger.LogDebug("Querying stores for seller with navigation properties. SellerId: {SellerId}", sellerId);

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(s => s.SellerId == sellerId)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} stores for seller: {SellerId}", stores.Count, sellerId);

                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve stores for seller: {SellerId}", sellerId);
                throw new InvalidOperationException($"Failed to retrieve stores for seller {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetStoresByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve stores by category: {Category}", category);
            
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(category))
                {
                    _logger.LogWarning("Attempted to retrieve stores with empty or null category");
                    return new List<Store>(); // Invalid category, return empty list
                }

                _logger.LogDebug("Querying stores by category with navigation properties. Category: {Category}", category);

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(s => s.Category == category)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} stores for category: {Category}", stores.Count, category);

                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve stores for category: {Category}", category);
                throw new InvalidOperationException($"Failed to retrieve stores for category {category}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetFilteredStoresAsync(Expression<Func<Store, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered stores");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve stores with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Querying filtered stores with navigation properties");

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} filtered stores", stores.Count);

                return stores;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered stores");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered stores");
                throw new InvalidOperationException($"Failed to retrieve filtered stores: {ex.Message}", ex);
            }
        }

        public async Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count products for store: {StoreId}", storeId);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to count products with empty store ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Counting products for store. StoreId: {StoreId}", storeId);

                var count = await _context.Products
                    .CountAsync(p => p.StoreId == storeId, cancellationToken);

                _logger.LogDebug("Successfully counted products for store {StoreId}: {ProductCount}", storeId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count products for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to count products for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetActiveStoresAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve active stores");
            
            try
            {
                _logger.LogDebug("Querying active stores with navigation properties");

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(s => s.IsActive && !s.IsDeleted)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} active stores", stores.Count);

                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active stores");
                throw new InvalidOperationException($"Failed to retrieve active stores: {ex.Message}", ex);
            }
        }
    }
}
