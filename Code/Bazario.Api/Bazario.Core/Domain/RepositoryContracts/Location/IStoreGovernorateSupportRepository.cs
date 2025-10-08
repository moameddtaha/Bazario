using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;

namespace Bazario.Core.Domain.RepositoryContracts.Location
{
    /// <summary>
    /// Repository contract for managing store-governorate shipping support relationships
    /// </summary>
    public interface IStoreGovernorateSupportRepository
    {
        /// <summary>
        /// Gets all governorate support records for a specific store
        /// </summary>
        Task<List<StoreGovernorateSupport>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all supported governorates for a specific store (IsSupported = true)
        /// </summary>
        Task<List<StoreGovernorateSupport>> GetSupportedGovernorates(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all excluded governorates for a specific store (IsSupported = false)
        /// </summary>
        Task<List<StoreGovernorateSupport>> GetExcludedGovernorates(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a store supports shipping to a specific governorate
        /// </summary>
        Task<bool> IsGovernorateSupportedAsync(Guid storeId, Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific support record by store and governorate IDs
        /// </summary>
        Task<StoreGovernorateSupport?> GetByStoreAndGovernorateAsync(Guid storeId, Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new store-governorate support record
        /// </summary>
        Task<StoreGovernorateSupport> AddAsync(StoreGovernorateSupport support, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple store-governorate support records in bulk
        /// </summary>
        Task AddRangeAsync(List<StoreGovernorateSupport> supports, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing support record (e.g., toggle IsSupported flag)
        /// </summary>
        Task<StoreGovernorateSupport> UpdateAsync(StoreGovernorateSupport support, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a specific support record
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all support records for a specific store
        /// </summary>
        Task DeleteByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces all governorate support records for a store (used when updating store shipping configuration)
        /// </summary>
        Task ReplaceStoreGovernorates(Guid storeId, List<StoreGovernorateSupport> newSupports, CancellationToken cancellationToken = default);
    }
}
