using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Enums;

namespace Bazario.Core.Helpers.Order
{
    /// <summary>
    /// Service for calculating shipping zones and delivery multipliers
    /// </summary>
    public interface IShippingZoneService
    {
        /// <summary>
        /// Determines the shipping zone for a given address
        /// </summary>
        Task<ShippingZone> DetermineShippingZoneAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the delivery time multiplier for a shipping zone
        /// </summary>
        decimal GetZoneMultiplier(ShippingZone zone);

        /// <summary>
        /// Gets the estimated delivery time in hours for a shipping zone
        /// </summary>
        int GetEstimatedDeliveryHours(ShippingZone zone);

        /// <summary>
        /// Checks if an address is eligible for express delivery
        /// </summary>
        Task<bool> IsEligibleForExpressDeliveryAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an address is eligible for same-day delivery
        /// </summary>
        Task<bool> IsEligibleForSameDayDeliveryAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default);
    }

}
