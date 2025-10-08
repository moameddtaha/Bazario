using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.City;

namespace Bazario.Core.ServiceContracts.Location
{
    /// <summary>
    /// Service contract for city management operations
    /// </summary>
    public interface ICityManagementService
    {
        /// <summary>
        /// Creates a new city
        /// </summary>
        Task<CityResponse> CreateCityAsync(CityAddRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing city
        /// </summary>
        Task<CityResponse> UpdateCityAsync(CityUpdateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a city by ID
        /// </summary>
        Task<CityResponse?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cities for a specific governorate
        /// </summary>
        Task<List<CityResponse>> GetCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets only active cities for a specific governorate
        /// </summary>
        Task<List<CityResponse>> GetActiveCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cities across all governorates
        /// </summary>
        Task<List<CityResponse>> GetAllCitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for cities by name (case-insensitive, partial match)
        /// </summary>
        Task<List<CityResponse>> SearchCitiesAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cities that support same-day delivery in a governorate
        /// </summary>
        Task<List<CityResponse>> GetSameDayDeliveryCitiesAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a city (soft delete)
        /// </summary>
        Task<bool> DeactivateCityAsync(Guid cityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a city exists by name within a governorate
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default);
    }
}
