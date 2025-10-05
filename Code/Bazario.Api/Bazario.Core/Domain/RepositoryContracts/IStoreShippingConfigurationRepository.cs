using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.Domain.RepositoryContracts
{
    /// <summary>
    /// Repository contract for store shipping configuration operations
    /// </summary>
    public interface IStoreShippingConfigurationRepository
    {
        Task<StoreShippingConfiguration?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
        Task<StoreShippingConfiguration> CreateAsync(StoreShippingConfiguration configuration, CancellationToken cancellationToken = default);
        Task<StoreShippingConfiguration> UpdateAsync(StoreShippingConfiguration configuration, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid configurationId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
