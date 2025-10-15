using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Order;
using ReviewEntity = Bazario.Core.Domain.Entities.Review.Review;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Models.Store;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Store
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

        public Task<StoreEntity> AddStoreAsync(StoreEntity store, CancellationToken cancellationToken = default)
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

                _logger.LogInformation("Successfully added store. StoreId: {StoreId}, Name: {StoreName}",
                    store.StoreId, store.Name);

                return Task.FromResult(store);
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

        public async Task<StoreEntity> UpdateStoreAsync(StoreEntity store, CancellationToken cancellationToken = default)
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

                // Check if store exists (respects query filter - won't find soft-deleted stores)
                var existingStore = await _context.Stores
                    .FirstOrDefaultAsync(s => s.StoreId == store.StoreId, cancellationToken);
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

                _logger.LogInformation("Successfully restored store. StoreId: {StoreId}, Name: {StoreName}", storeId, store.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while restoring store: {StoreId}", storeId);
                throw new InvalidOperationException($"Unexpected error while restoring store with ID {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<StoreEntity?> GetStoreByIdIncludeDeletedAsync(Guid storeId, CancellationToken cancellationToken = default)
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

        public async Task<StoreEntity?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
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


        public async Task<List<StoreEntity>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve stores for seller: {SellerId}", sellerId);
            
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve stores with empty seller ID");
                    return new List<StoreEntity>(); // Invalid ID, return empty list
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

        public async Task<List<StoreEntity>> GetAllStoresAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving all stores");
            
            try
            {
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
                throw new InvalidOperationException($"Failed to retrieve all stores: {ex.Message}", ex);
            }
        }


        public async Task<List<StoreEntity>> GetFilteredStoresAsync(Expression<Func<StoreEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving filtered stores with custom predicate");
            
            try
            {
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to filter stores with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {StoreCount} filtered stores", stores.Count);
                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered stores");
                throw new InvalidOperationException($"Failed to retrieve filtered stores: {ex.Message}", ex);
            }
        }

        public async Task<List<StoreEntity>> GetStoresByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve stores by category: {Category}", category);
            
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(category))
                {
                    _logger.LogWarning("Attempted to retrieve stores with empty or null category");
                    return new List<StoreEntity>(); // Invalid category, return empty list
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

        public async Task<List<StoreEntity>> GetActiveStoresAsync(CancellationToken cancellationToken = default)
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

        public IQueryable<StoreEntity> GetStoresQueryable()
        {
            _logger.LogDebug("Returning queryable for stores");
            
            try
            {
                // Return queryable with navigation properties
                // The HasQueryFilter will automatically be applied by EF Core
                return _context.Stores
                    .Include(s => s.Seller)
                    .Include(s => s.Products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stores queryable");
                throw new InvalidOperationException($"Failed to create stores queryable: {ex.Message}", ex);
            }
        }

        public async Task<int> GetStoresCountAsync(IQueryable<StoreEntity> query, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting count of stores from query");
            
            try
            {
                return await query.CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stores count");
                throw new InvalidOperationException($"Failed to get stores count: {ex.Message}", ex);
            }
        }

        public async Task<List<StoreEntity>> GetStoresPagedAsync(IQueryable<StoreEntity> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting paged stores from query. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            
            try
            {
                return await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get paged stores");
                throw new InvalidOperationException($"Failed to get paged stores: {ex.Message}", ex);
            }
        }

        public IQueryable<StoreEntity> GetStoresQueryableIgnoreFilters()
        {
            _logger.LogDebug("Returning queryable for stores (ignoring global filters)");
            
            try
            {
                // Return queryable with navigation properties, ignoring global query filters
                return _context.Stores
                    .IgnoreQueryFilters()
                    .Include(s => s.Seller)
                    .Include(s => s.Products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stores queryable (ignoring filters)");
                throw new InvalidOperationException($"Failed to create stores queryable (ignoring filters): {ex.Message}", ex);
            }
        }

        public async Task<List<StorePerformance>> GetTopPerformingStoresAsync(IQueryable<StoreEntity> query, int pageNumber, int pageSize, string performanceCriteria, DateTime? performancePeriodStart = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting top performing stores with criteria: {PerformanceCriteria}, page: {PageNumber}, size: {PageSize}", 
                performanceCriteria, pageNumber, pageSize);

            try
            {
                // First, get all matching stores with their related data using efficient Include
                // This approach avoids complex EF translation issues while still being performant
                // Only include recent data for performance metrics (configurable period, default 12 months)
                var periodStart = performancePeriodStart ?? DateTime.UtcNow.AddMonths(-12);
                
                _logger.LogDebug("Using performance period: {PeriodStart} to {PeriodEnd}", 
                    periodStart, DateTime.UtcNow);
                
                var queryWithIncludes = query
                    .Include(s => s.Products!.Where(p => !p.IsDeleted))
                        .ThenInclude(p => p.OrderItems!.Where(oi => oi.Order != null && oi.Order.Date >= periodStart))
                            .ThenInclude(oi => oi.Order) // Include Order for the Date filter
                    .Include(s => s.Products!)
                        .ThenInclude(p => p.Reviews!.Where(r => r.CreatedAt >= periodStart))
                    .AsNoTracking();

                // Log the generated SQL for debugging (only in debug builds)
                #if DEBUG
                var sqlQuery = queryWithIncludes.ToQueryString();
                _logger.LogDebug("Generated SQL Query: {SQL}", sqlQuery);
                #endif

                // Add safety limit for in-memory operations to prevent memory issues
                var storesWithData = await queryWithIncludes.ToListAsync(cancellationToken);
                
                // Check for large datasets and warn if necessary
                if (storesWithData.Count > 1000)
                {
                    _logger.LogWarning("Large dataset detected ({Count} stores). Consider using database-level aggregation for better performance. Current approach may consume significant memory.", storesWithData.Count);
                }
                else if (storesWithData.Count > 500)
                {
                    _logger.LogInformation("Moderate dataset size ({Count} stores). Performance should be acceptable but monitor memory usage.", storesWithData.Count);
                }

                _logger.LogDebug("Retrieved {StoreCount} stores with related data for performance calculation", storesWithData.Count);

                // Calculate performance metrics in memory (acceptable for filtered datasets)
                // Data is already filtered at database level, so no need to filter again
                var performances = storesWithData.Select(s => new StorePerformance
                {
                    StoreId = s.StoreId,
                    StoreName = s.Name,
                    Category = s.Category,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt ?? DateTime.UtcNow,
                    ProductCount = s.Products?.Count(p => !p.IsDeleted) ?? 0,
                    // No need to filter again - already filtered in Include statements
                    TotalRevenue = s.Products?.SelectMany(p => p.OrderItems ?? new List<OrderItem>())
                                             .Sum(oi => oi.Price * oi.Quantity) ?? 0,
                    // No need to filter again - already filtered in Include statements
                    TotalOrders = s.Products?.SelectMany(p => p.OrderItems ?? new List<OrderItem>())
                                            .Select(oi => oi.OrderId)
                                            .Distinct()
                                            .Count() ?? 0,
                    // Only keep the rating > 0 filter since date filtering is already done
                    AverageRating = s.Products?.SelectMany(p => p.Reviews ?? new List<ReviewEntity>())
                                              .Where(r => r.Rating > 0)
                                              .Average(r => (decimal?)r.Rating) ?? 0,
                    // No need to filter again - already filtered in Include statements
                    ReviewCount = s.Products?.SelectMany(p => p.Reviews ?? new List<ReviewEntity>())
                                            .Count() ?? 0,
                    Rank = 0 // Will be set after sorting and pagination
                }).ToList();

                _logger.LogDebug("Calculated performance metrics for {PerformanceCount} stores", performances.Count);

                // Sort by the actual performance criteria
                var sorted = performanceCriteria.ToLower() switch
                {
                    "revenue" => performances.OrderByDescending(s => s.TotalRevenue),
                    "orders" => performances.OrderByDescending(s => s.TotalOrders),
                    "rating" => performances.OrderByDescending(s => s.AverageRating),
                    "customers" => performances.OrderByDescending(s => s.ReviewCount),
                    "products" => performances.OrderByDescending(s => s.ProductCount),
                    _ => performances.OrderByDescending(s => s.TotalRevenue)
                };

                // Apply pagination and set global rankings
                var results = sorted
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select((perf, index) => 
                    {
                        perf.Rank = (pageNumber - 1) * pageSize + index + 1;
                        return perf;
                    })
                    .ToList();

                _logger.LogDebug("Successfully retrieved {Count} top performing stores with criteria: {PerformanceCriteria}. Global rankings: {StartRank}-{EndRank}", 
                    results.Count, performanceCriteria, results.FirstOrDefault()?.Rank ?? 0, results.LastOrDefault()?.Rank ?? 0);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get top performing stores with criteria: {PerformanceCriteria}", performanceCriteria);
                throw new InvalidOperationException($"Failed to get top performing stores: {ex.Message}", ex);
            }
        }

        public async Task<List<StoreEntity>> GetStoresByIdsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting stores by IDs: {StoreIds}", string.Join(", ", storeIds));

            try
            {
                if (storeIds == null || !storeIds.Any())
                {
                    _logger.LogWarning("Store IDs list is null or empty");
                    return new List<StoreEntity>();
                }

                var stores = await _context.Stores
                    .Include(s => s.Seller)
                    .Where(s => storeIds.Contains(s.StoreId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} stores for {StoreIdCount} IDs", stores.Count, storeIds.Count);
                return stores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stores by IDs");
                throw new InvalidOperationException($"Failed to get stores by IDs: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsStoreNameTakenAsync(string storeName, Guid? excludeStoreId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if store name is taken: {StoreName} (excluding: {ExcludeId})", storeName, excludeStoreId);

            try
            {
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    _logger.LogWarning("Store name is null or empty");
                    return false; // Empty name is not "taken"
                }

                var query = _context.Stores
                    .Where(s => s.Name != null && s.Name.ToLower() == storeName.ToLower());

                // Exclude specific store ID (for update scenarios)
                if (excludeStoreId.HasValue && excludeStoreId.Value != Guid.Empty)
                {
                    query = query.Where(s => s.StoreId != excludeStoreId.Value);
                }

                var exists = await query.AnyAsync(cancellationToken);

                _logger.LogDebug("Store name '{StoreName}' is {Status}", storeName, exists ? "taken" : "available");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if store name is taken: {StoreName}", storeName);
                throw new InvalidOperationException($"Failed to check store name availability: {ex.Message}", ex);
            }
        }
    }
}
