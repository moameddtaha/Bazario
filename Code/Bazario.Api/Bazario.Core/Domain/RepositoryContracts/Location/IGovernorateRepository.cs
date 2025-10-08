using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;

namespace Bazario.Core.Domain.RepositoryContracts.Location
{
    /// <summary>
    /// Repository contract for Governorate entity operations
    /// </summary>
    public interface IGovernorateRepository
    {
        /// <summary>
        /// Gets a governorate by its ID
        /// </summary>
        Task<Governorate?> GetByIdAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all governorates for a specific country
        /// </summary>
        Task<List<Governorate>> GetByCountryIdAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active governorates for a specific country
        /// </summary>
        Task<List<Governorate>> GetActiveByCountryIdAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets governorates by a list of IDs
        /// </summary>
        Task<List<Governorate>> GetByIdsAsync(List<Guid> governorateIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a governorate by name and country
        /// </summary>
        Task<Governorate?> GetByNameAndCountryAsync(string name, Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all governorates
        /// </summary>
        Task<List<Governorate>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all governorates that support same-day delivery
        /// </summary>
        Task<List<Governorate>> GetSameDayDeliveryGovernoratesAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new governorate
        /// </summary>
        Task<Governorate> AddAsync(Governorate governorate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing governorate
        /// </summary>
        Task<Governorate> UpdateAsync(Governorate governorate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a governorate (soft delete)
        /// </summary>
        Task<bool> DeactivateAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a governorate name already exists in a country
        /// </summary>
        Task<bool> ExistsByNameInCountryAsync(string name, Guid countryId, CancellationToken cancellationToken = default);
    }
}
