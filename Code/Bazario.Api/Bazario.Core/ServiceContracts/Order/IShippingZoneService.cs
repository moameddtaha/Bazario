using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service for calculating store-specific shipping zones and delivery multipliers.
    /// Supports both store-specific configurations from database and fallback logic for general use.
    /// </summary>
    public interface IShippingZoneService
    {
        /// <summary>
        /// Gets the delivery time multiplier for a shipping zone.
        /// </summary>
        /// <param name="zone">The shipping zone</param>
        /// <returns>Multiplier value (e.g., 0.3 for SameDay, 1.0 for Local, 2.0 for National)</returns>
        /// <remarks>Optimized for Egypt but scalable for future expansion</remarks>
        decimal GetZoneMultiplier(ShippingZone zone);

        /// <summary>
        /// Gets the estimated delivery time in hours for a shipping zone.
        /// </summary>
        /// <param name="zone">The shipping zone</param>
        /// <returns>Estimated delivery hours (e.g., 4 for SameDay, 24 for Local, 48 for National)</returns>
        /// <remarks>Optimized for Egypt but scalable for future expansion</remarks>
        int GetEstimatedDeliveryHours(ShippingZone zone);

        /// <summary>
        /// Determines the shipping zone for a specific store and city using database configuration.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The appropriate shipping zone for the store and location</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty</exception>
        /// <remarks>
        /// Resolves city to governorate, checks store's supported governorates,
        /// and determines appropriate zone based on store configuration and location.
        /// </remarks>
        Task<ShippingZone> DetermineStoreShippingZoneAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a city is eligible for same-day delivery from a specific store.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if same-day delivery is available; false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when error occurs checking eligibility</exception>
        /// <remarks>
        /// Checks store-specific configuration first, then falls back to simple eligibility check.
        /// </remarks>
        Task<bool> IsEligibleForStoreSameDayDeliveryAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the delivery fee for a specific store and city based on shipping zone.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The delivery fee in the platform's base currency; 0 if zone is not supported</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when error occurs calculating fee</exception>
        /// <remarks>
        /// Determines shipping zone first, then retrieves store-specific fee from database configuration.
        /// </remarks>
        Task<decimal> GetStoreDeliveryFeeAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available delivery options for a specific store and city.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available shipping zones for the store and location</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty</exception>
        /// <remarks>
        /// Checks store-specific same-day delivery availability and adds appropriate fallback zones.
        /// Returns NotSupported if no delivery options are available.
        /// </remarks>
        Task<List<ShippingZone>> GetAvailableDeliveryOptionsAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        #region Fallback Methods (No Store ID Required)

        /// <summary>
        /// Determines shipping zone using simple fallback logic when store ID is not available.
        /// </summary>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <returns>The appropriate shipping zone based on city and country</returns>
        /// <remarks>
        /// Uses hardcoded logic for Egypt: Cairo = SameDay, major cities = National, others = National.
        /// Returns NotSupported for non-EG countries.
        /// </remarks>
        ShippingZone DetermineShippingZoneFallback(string city, string country);

        /// <summary>
        /// Checks if a city is eligible for same-day delivery using simple fallback logic.
        /// </summary>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <returns>True if same-day delivery is available; false otherwise</returns>
        /// <remarks>
        /// Currently only Cairo, Egypt is eligible for same-day delivery in fallback logic.
        /// </remarks>
        bool IsEligibleForSameDayDeliveryFallback(string city, string country);

        /// <summary>
        /// Gets delivery fee using simple fallback logic when store ID is not available.
        /// </summary>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <returns>Delivery fee; returns 0 as stores must configure their own fees</returns>
        /// <remarks>
        /// Returns 0 for all zones as this is fallback logic.
        /// Stores should configure their own delivery fees in the database.
        /// </remarks>
        decimal GetDeliveryFeeFallback(string city, string country);

        /// <summary>
        /// Gets delivery fee using store-specific configuration when store ID is available.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store</param>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery fee based on fallback zone and store configuration</returns>
        /// <exception cref="ArgumentException">Thrown when storeId is empty</exception>
        /// <remarks>
        /// Determines zone using fallback logic, then retrieves store-specific fee from database.
        /// </remarks>
        Task<decimal> GetStoreDeliveryFeeFallbackAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available delivery options using simple fallback logic when store ID is not available.
        /// </summary>
        /// <param name="city">The city name</param>
        /// <param name="country">The country code (e.g., "EG" for Egypt)</param>
        /// <returns>List containing single fallback shipping zone</returns>
        /// <remarks>
        /// Returns a list with only the fallback zone for the given city and country.
        /// </remarks>
        List<ShippingZone> GetAvailableDeliveryOptionsFallback(string city, string country);

        #endregion
    }
}
