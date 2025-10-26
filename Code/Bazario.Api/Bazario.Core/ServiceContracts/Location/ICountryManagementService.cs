using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.Country;

namespace Bazario.Core.ServiceContracts.Location
{
    /// <summary>
    /// Service contract for country management operations
    /// </summary>
    public interface ICountryManagementService
    {
        /// <summary>
        /// Creates a new country (Admin only)
        /// </summary>
        /// <param name="request">Country creation request</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<CountryResponse> CreateCountryAsync(CountryAddRequest request, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing country (Admin only)
        /// </summary>
        /// <param name="request">Country update request</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<CountryResponse> UpdateCountryAsync(CountryUpdateRequest request, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a country by ID with governorate count
        /// </summary>
        Task<CountryResponse?> GetCountryByIdAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a country by code (e.g., "EG", "SA")
        /// </summary>
        Task<CountryResponse?> GetCountryByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all countries
        /// </summary>
        Task<List<CountryResponse>> GetAllCountriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets only active countries
        /// </summary>
        Task<List<CountryResponse>> GetActiveCountriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a country (Admin only)
        /// </summary>
        /// <param name="countryId">Country ID to deactivate</param>
        /// <param name="userId">User ID performing the operation (must have admin privileges)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task<bool> DeactivateCountryAsync(Guid countryId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a country exists by code
        /// </summary>
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a country exists by name
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
