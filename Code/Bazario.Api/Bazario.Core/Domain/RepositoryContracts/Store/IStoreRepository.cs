using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Store;
using Bazario.Core.Models.Store;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;

namespace Bazario.Core.Domain.RepositoryContracts.Store
{
    public interface IStoreRepository
    {
        Task<StoreEntity> AddStoreAsync(StoreEntity store, CancellationToken cancellationToken = default);

        Task<StoreEntity> UpdateStoreAsync(StoreEntity store, CancellationToken cancellationToken = default);

        Task<bool> DeleteStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a store by setting IsDeleted = true
        /// </summary>
        Task<bool> SoftDeleteStoreAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted store by setting IsDeleted = false
        /// </summary>
        Task<bool> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a store by ID including soft-deleted stores (ignores query filter)
        /// </summary>
        Task<StoreEntity?> GetStoreByIdIncludeDeletedAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<StoreEntity?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<List<StoreEntity>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<List<StoreEntity>> GetAllStoresAsync(CancellationToken cancellationToken = default);

        Task<List<StoreEntity>> GetActiveStoresAsync(CancellationToken cancellationToken = default);

        Task<List<StoreEntity>> GetFilteredStoresAsync(Expression<Func<StoreEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for stores to enable efficient filtering and pagination
        /// </summary>
        IQueryable<StoreEntity> GetStoresQueryable();

        /// <summary>
        /// Gets the count of stores matching the query
        /// </summary>
        Task<int> GetStoresCountAsync(IQueryable<StoreEntity> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stores with pagination from the query
        /// </summary>
        Task<List<StoreEntity>> GetStoresPagedAsync(IQueryable<StoreEntity> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for stores that ignores global query filters (for soft deletion scenarios)
        /// </summary>
        IQueryable<StoreEntity> GetStoresQueryableIgnoreFilters();

        /// <summary>
        /// Gets top performing stores with performance metrics calculated at database level
        /// </summary>
        Task<List<StorePerformance>> GetTopPerformingStoresAsync(IQueryable<StoreEntity> query, int pageNumber, int pageSize, string performanceCriteria, DateTime? performancePeriodStart = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple stores by their IDs in a single query to avoid N+1 problem.
        /// </summary>
        Task<List<StoreEntity>> GetStoresByIdsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default);
    }
}
