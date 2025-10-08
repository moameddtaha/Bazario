using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.Governorate;

namespace Bazario.Core.ServiceContracts.Location
{
    /// <summary>
    /// Service contract for governorate/state management operations
    /// </summary>
    public interface IGovernorateManagementService
    {
        /// <summary>
        /// Creates a new governorate
        /// </summary>
        Task<GovernorateResponse> CreateGovernorateAsync(GovernorateAddRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing governorate
        /// </summary>
        Task<GovernorateResponse> UpdateGovernorateAsync(GovernorateUpdateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a governorate by ID with city count
        /// </summary>
        Task<GovernorateResponse?> GetGovernorateByIdAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all governorates for a specific country
        /// </summary>
        Task<List<GovernorateResponse>> GetGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets only active governorates for a specific country
        /// </summary>
        Task<List<GovernorateResponse>> GetActiveGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all governorates across all countries
        /// </summary>
        Task<List<GovernorateResponse>> GetAllGovernoratesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets governorates that support same-day delivery
        /// </summary>
        Task<List<GovernorateResponse>> GetSameDayDeliveryGovernoratesAsync(Guid countryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a governorate (soft delete)
        /// </summary>
        Task<bool> DeactivateGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a governorate exists by name within a country
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid countryId, CancellationToken cancellationToken = default);
    }
}
