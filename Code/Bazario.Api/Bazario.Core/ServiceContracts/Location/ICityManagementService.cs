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
        /// Creates a new city (Admin only)
        /// </summary>
        /// <param name="request">City creation request</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<CityResponse> CreateCityAsync(CityAddRequest request, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing city (Admin only)
        /// </summary>
        /// <param name="request">City update request</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<CityResponse> UpdateCityAsync(CityUpdateRequest request, Guid userId, CancellationToken cancellationToken = default);

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
        /// Deactivates a city (Admin only)
        /// </summary>
        /// <param name="cityId">City ID to deactivate</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<bool> DeactivateCityAsync(Guid cityId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a city exists by name within a governorate
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default);
    }
}
