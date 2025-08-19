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

namespace Bazario.Infrastructure.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly ApplicationDbContext _context;

        public StoreRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Store> AddStoreAsync(Store store, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (store == null)
                    throw new ArgumentNullException(nameof(store));

                // Add store to context
                _context.Stores.Add(store);
                await _context.SaveChangesAsync(cancellationToken);

                return store;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating store: {ex.Message}", ex);
            }
        }

        public async Task<Store> UpdateStoreAsync(Store store, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (store == null)
                    throw new ArgumentNullException(nameof(store));

                if (store.StoreId == Guid.Empty)
                    throw new ArgumentException("Store ID cannot be empty", nameof(store));

                // Check if store exists (use FindAsync for simple PK lookup)
                var existingStore = await _context.Stores.FindAsync(new object[] { store.StoreId }, cancellationToken);
                if (existingStore == null)
                {
                    throw new InvalidOperationException($"Store with ID {store.StoreId} not found");
                }

                // Update only specific properties (not foreign keys or primary key)
                existingStore.Name = store.Name;
                existingStore.Description = store.Description;
                existingStore.Category = store.Category;
                existingStore.Logo = store.Logo;
                
                await _context.SaveChangesAsync(cancellationToken);

                return existingStore;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while updating store with ID {store?.StoreId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var store = await _context.Stores.FindAsync(new object[] { storeId }, cancellationToken);
                if (store == null)
                {
                    return false; // Store not found
                }

                // Delete the store
                _context.Stores.Remove(store);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
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
                throw new InvalidOperationException($"Failed to retrieve store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetAllStoresAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .ToListAsync(cancellationToken);

                return stores;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve stores: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    return new List<Store>(); // Invalid ID, return empty list
                }

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(s => s.SellerId == sellerId)
                    .ToListAsync(cancellationToken);

                return stores;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve stores for seller {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetStoresByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(category))
                {
                    return new List<Store>(); // Invalid category, return empty list
                }

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(s => s.Category == category)
                    .ToListAsync(cancellationToken);

                return stores;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve stores for category {category}: {ex.Message}", ex);
            }
        }

        public async Task<List<Store>> GetFilteredStoresAsync(Expression<Func<Store, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                return stores;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered stores: {ex.Message}", ex);
            }
        }

        public async Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var count = await _context.Products
                    .CountAsync(p => p.StoreId == storeId, cancellationToken);

                return count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count products for store {storeId}: {ex.Message}", ex);
            }
        }
    }
}
