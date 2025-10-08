using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;

namespace Bazario.Core.Domain.RepositoryContracts.Location
{
    /// <summary>
    /// Repository contract for city operations
    /// </summary>
    public interface ICityRepository
    {
        /// <summary>
        /// Gets a city by ID
        /// </summary>
        Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a city by name and governorate
        /// </summary>
        Task<City?> GetByNameAndGovernorateAsync(string name, Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cities
        /// </summary>
        Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cities in a specific governorate
        /// </summary>
        Task<List<City>> GetByGovernorateIdAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active cities in a specific governorate
        /// </summary>
        Task<List<City>> GetActiveByGovernorateIdAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cities that support same-day delivery in a governorate
        /// </summary>
        Task<List<City>> GetSameDayDeliveryCitiesAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for a city by name (case-insensitive, partial match)
        /// </summary>
        Task<List<City>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new city
        /// </summary>
        Task<City> AddAsync(City city, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing city
        /// </summary>
        Task<City> UpdateAsync(City city, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a city (soft delete)
        /// </summary>
        Task<bool> DeactivateAsync(Guid cityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a city exists by name in a governorate
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default);
    }
}
