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
        /// Creates a new country
        /// </summary>
        Task<CountryResponse> CreateCountryAsync(CountryAddRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing country
        /// </summary>
        Task<CountryResponse> UpdateCountryAsync(CountryUpdateRequest request, CancellationToken cancellationToken = default);

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
        /// Deactivates a country (soft delete)
        /// </summary>
        Task<bool> DeactivateCountryAsync(Guid countryId, CancellationToken cancellationToken = default);

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
