using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface IStoreRepository
    {
        Task<Store> AddStoreAsync(Store store, CancellationToken cancellationToken = default);

        Task<Store> UpdateStoreAsync(Store store, CancellationToken cancellationToken = default);

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
        Task<Store?> GetStoreByIdIncludeDeletedAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<List<Store>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<List<Store>> GetAllStoresAsync(CancellationToken cancellationToken = default);

        Task<List<Store>> GetActiveStoresAsync(CancellationToken cancellationToken = default);

        Task<List<Store>> GetFilteredStoresAsync(Expression<Func<Store, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for stores to enable efficient filtering and pagination
        /// </summary>
        IQueryable<Store> GetStoresQueryable();

        /// <summary>
        /// Gets the count of stores matching the query
        /// </summary>
        Task<int> GetStoresCountAsync(IQueryable<Store> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stores with pagination from the query
        /// </summary>
        Task<List<Store>> GetStoresPagedAsync(IQueryable<Store> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for stores that ignores global query filters (for soft deletion scenarios)
        /// </summary>
        IQueryable<Store> GetStoresQueryableIgnoreFilters();

        /// <summary>
        /// Gets top performing stores with performance metrics calculated at database level
        /// </summary>
        Task<List<StorePerformance>> GetTopPerformingStoresAsync(IQueryable<Store> query, int pageNumber, int pageSize, string performanceCriteria, DateTime? performancePeriodStart = null, CancellationToken cancellationToken = default);
    }
}
