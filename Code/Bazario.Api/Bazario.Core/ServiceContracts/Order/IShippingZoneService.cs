using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service for calculating store-specific shipping zones and delivery multipliers
    /// </summary>
    public interface IShippingZoneService
    {
        /// <summary>
        /// Gets the delivery time multiplier for a shipping zone
        /// </summary>
        decimal GetZoneMultiplier(ShippingZone zone);

        /// <summary>
        /// Gets the estimated delivery time in hours for a shipping zone
        /// </summary>
        int GetEstimatedDeliveryHours(ShippingZone zone);

        /// <summary>
        /// Determines the shipping zone for a specific store and city
        /// </summary>
        Task<ShippingZone> DetermineStoreShippingZoneAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a city is eligible for same-day delivery from a specific store
        /// </summary>
        Task<bool> IsEligibleForStoreSameDayDeliveryAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets the delivery fee for a specific store and city
        /// </summary>
        Task<decimal> GetStoreDeliveryFeeAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available delivery options for a specific store and city
        /// </summary>
        Task<List<ShippingZone>> GetAvailableDeliveryOptionsAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default);

        #region Fallback Methods (No Store ID Required)

        /// <summary>
        /// Determines shipping zone using simple fallback logic when store ID is not available
        /// </summary>
        ShippingZone DetermineShippingZoneFallback(string city, string country);

        /// <summary>
        /// Checks if a city is eligible for same-day delivery using simple fallback logic
        /// </summary>
        bool IsEligibleForSameDayDeliveryFallback(string city, string country);


        /// <summary>
        /// Gets delivery fee using simple fallback logic when store ID is not available
        /// </summary>
        decimal GetDeliveryFeeFallback(string city, string country);

        /// <summary>
        /// Gets available delivery options using simple fallback logic when store ID is not available
        /// </summary>
        List<ShippingZone> GetAvailableDeliveryOptionsFallback(string city, string country);

        #endregion
    }
}
