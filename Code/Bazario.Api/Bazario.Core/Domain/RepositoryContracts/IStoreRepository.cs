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

        Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<List<Store>> GetAllStoresAsync(CancellationToken cancellationToken = default);

        Task<List<Store>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<List<Store>> GetStoresByCategoryAsync(string category, CancellationToken cancellationToken = default);

        Task<List<Store>> GetFilteredStoresAsync(Expression<Func<Store, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> GetProductCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
