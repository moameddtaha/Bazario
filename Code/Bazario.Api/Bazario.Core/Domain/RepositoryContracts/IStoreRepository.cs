using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

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

        Task<List<Store>> GetAllStoresAsync(CancellationToken cancellationToken = default);

        Task<List<Store>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<List<Store>> GetStoresByCategoryAsync(string category, CancellationToken cancellationToken = default);

        Task<List<Store>> GetActiveStoresAsync(CancellationToken cancellationToken = default);

        Task<List<Store>> GetFilteredStoresAsync(Expression<Func<Store, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
