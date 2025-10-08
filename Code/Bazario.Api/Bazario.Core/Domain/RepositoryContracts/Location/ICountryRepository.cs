using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;

namespace Bazario.Core.Domain.RepositoryContracts.Location
{
    /// <summary>
    /// Repository contract for Country entity operations
    /// </summary>
    public interface ICountryRepository
    {
        /// <summary>
        /// Gets a country by its ID
        /// </summary>
        Task<Country?> GetByIdAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a country by its code (e.g., "EG", "SA")
        /// </summary>
        Task<Country?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all countries
        /// </summary>
        Task<List<Country>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active countries
        /// </summary>
        Task<List<Country>> GetActiveCountriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new country
        /// </summary>
        Task<Country> AddAsync(Country country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing country
        /// </summary>
        Task<Country> UpdateAsync(Country country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a country (soft delete)
        /// </summary>
        Task<bool> DeactivateAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a country code already exists
        /// </summary>
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a country name already exists
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
