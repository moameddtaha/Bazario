using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Service contract for managing store shipping and delivery configurations.
    /// Handles delivery types, geographic coverage, fees, cutoff times, and availability checks.
    /// </summary>
    public interface IStoreShippingConfigurationService
    {
        /// <summary>
        /// Retrieves the shipping configuration for a specific store including delivery options, fees, and geographic coverage.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// The store's shipping configuration, or a default inactive configuration if none exists.
        /// </returns>
        Task<StoreShippingConfigurationResponse> GetConfigurationAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new shipping configuration for a store with delivery options, fees, and geographic coverage.
        /// </summary>
        /// <param name="request">Configuration details including delivery types, fees, and supported governorates</param>
        /// <param name="userId">The user ID creating the configuration (must be store owner or admin)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The newly created shipping configuration</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the store does not exist or already has a configuration.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the user is not authorized to create configuration for this store.
        /// </exception>
        Task<StoreShippingConfigurationResponse> CreateConfigurationAsync(StoreShippingConfigurationRequest request, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing shipping configuration for a store using a replace strategy for governorate associations.
        /// </summary>
        /// <param name="request">Updated configuration details</param>
        /// <param name="userId">The user ID updating the configuration (must be store owner or admin)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated shipping configuration</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no configuration exists for the store.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the user is not authorized to update configuration for this store.
        /// </exception>
        /// <remarks>
        /// Replaces all governorate associations to prevent orphaned records.
        /// </remarks>
        Task<StoreShippingConfigurationResponse> UpdateConfigurationAsync(StoreShippingConfigurationRequest request, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a shipping configuration from the database. Requires administrator privileges.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="deletedBy">The administrator user ID performing the deletion</param>
        /// <param name="reason">The reason for deletion (required for audit trail)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted; false if configuration not found</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when storeId, deletedBy, or reason is empty.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the user does not have administrator privileges.
        /// </exception>
        /// <remarks>
        /// This operation is restricted to administrators due to data integrity concerns.
        /// Past orders may reference this configuration for fee calculations and historical records.
        /// This operation is irreversible and logged as critical.
        /// </remarks>
        Task<bool> DeleteConfigurationAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if same-day delivery is available for a store to a specific city based on configuration, governorate support, and cutoff time.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name to check delivery availability for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// True if same-day delivery is available; false otherwise or on error.
        /// </returns>
        /// <remarks>
        /// Validates store configuration, city-to-governorate resolution, governorate support, infrastructure capability, and cutoff time.
        /// Returns false if any validation fails or if the current time exceeds the configured cutoff hour.
        /// </remarks>
        Task<bool> IsSameDayDeliveryAvailableAsync(Guid storeId, string city, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the delivery fee for a store to a specific city, returning either same-day or standard rate based on availability.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name to calculate delivery fee for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// The delivery fee in the platform's base currency. Returns 0 if no configuration exists or on error.
        /// </returns>
        /// <remarks>
        /// Returns SameDayDeliveryFee if same-day delivery is available, otherwise returns StandardDeliveryFee.
        /// </remarks>
        Task<decimal> GetDeliveryFeeAsync(Guid storeId, string city, CancellationToken cancellationToken = default);
    }
}
