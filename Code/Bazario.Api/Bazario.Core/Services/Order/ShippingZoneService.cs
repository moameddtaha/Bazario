using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.ServiceContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Production-ready shipping zone service that calculates delivery zones based on real address data
    /// </summary>
    public class ShippingZoneService : IShippingZoneService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ShippingZoneService> _logger;
        private readonly IStoreShippingConfigurationService _storeShippingConfigurationService;
        private readonly IStoreGovernorateSupportRepository _governorateSupportRepository;
        private readonly ICityRepository _cityRepository;

        public ShippingZoneService(
            IConfiguration configuration,
            ILogger<ShippingZoneService> logger,
            IStoreShippingConfigurationService storeShippingConfigurationService,
            IStoreGovernorateSupportRepository governorateSupportRepository,
            ICityRepository cityRepository)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storeShippingConfigurationService = storeShippingConfigurationService ?? throw new ArgumentNullException(nameof(storeShippingConfigurationService));
            _governorateSupportRepository = governorateSupportRepository ?? throw new ArgumentNullException(nameof(governorateSupportRepository));
            _cityRepository = cityRepository ?? throw new ArgumentNullException(nameof(cityRepository));
        }


        /// <summary>
        /// Production-ready method to resolve city name to governorate ID via database lookup
        /// </summary>
        private async Task<Guid?> ResolveGovernorateFromCityAsync(string cityName, string country, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(country) || country.ToUpperInvariant() != "EG")
            {
                _logger.LogDebug("Country {Country} not supported for governorate resolution", country);
                return null;
            }

            if (string.IsNullOrWhiteSpace(cityName))
            {
                _logger.LogDebug("City name is empty, cannot resolve governorate");
                return null;
            }

            try
            {
                // Search for city in database (case-insensitive)
                var cities = await _cityRepository.SearchByNameAsync(cityName, cancellationToken);
                var city = cities.FirstOrDefault(c => c.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase));

                if (city == null)
                {
                    _logger.LogDebug("City {CityName} not found in database", cityName);
                    return null;
                }

                if (city.Governorate == null)
                {
                    _logger.LogWarning("City {CityName} found but has no governorate associated", cityName);
                    return null;
                }

                _logger.LogDebug("Resolved city {CityName} to governorate {GovernorateName} (ID: {GovernorateId})",
                    cityName, city.Governorate.Name, city.GovernorateId);

                return city.GovernorateId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving governorate for city: {CityName}", cityName);
                return null;
            }
        }

        /// <summary>
        /// Checks if a store supports shipping to a specific governorate
        /// </summary>
        private async Task<bool> IsGovernorateSupported(Guid storeId, Guid governorateId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _governorateSupportRepository.IsGovernorateSupportedAsync(storeId, governorateId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking governorate support for store {StoreId}, governorate {GovernorateId}", storeId, governorateId);
                return false;
            }
        }

        public decimal GetZoneMultiplier(ShippingZone zone)
        {
            // Delivery time multipliers optimized for Egypt but scalable for future expansion
            return zone switch
            {
                ShippingZone.SameDay => 0.3m,      // 3-4 hours for same-day delivery (major cities)
                ShippingZone.Local => 1.0m,         // 12-24 hours for local delivery
                ShippingZone.National => 2.0m,      // 24-48 hours for national delivery
                ShippingZone.NotSupported => 0.0m,  // Not supported - no multiplier
                _ => 1.0m                           // Default fallback
            };
        }

        public int GetEstimatedDeliveryHours(ShippingZone zone)
        {
            // Delivery time estimates optimized for Egypt but scalable for future expansion
            return zone switch
            {
                ShippingZone.SameDay => 4,          // Same day delivery (major cities)
                ShippingZone.Local => 24,           // 1 day delivery (same city)
                ShippingZone.National => 48,        // 2 day delivery (different region)
                ShippingZone.NotSupported => 0,     // Not supported - no delivery time
                _ => 24                             // Default 1 day delivery
            };
        }

        private ShippingZone GetSimpleFallbackZone(string city, string country)
        {
            // Simple fallback zone determination when store-specific logic fails
            if (string.IsNullOrWhiteSpace(country) || country.ToUpperInvariant() != "EG")
            {
                return ShippingZone.NotSupported;
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return ShippingZone.Local;
            }

            var cityUpper = city.ToUpperInvariant();
            
            // Same-day delivery cities (Cairo only)
            if (cityUpper == "CAIRO")
            {
                return ShippingZone.SameDay;
            }
            
            // Major cities (national delivery)
            if (cityUpper == "ALEXANDRIA" || cityUpper == "GIZA" || cityUpper == "PORT SAID" || cityUpper == "SUEZ" || 
                cityUpper == "LUXOR" || cityUpper == "ASWAN" || cityUpper == "HURGHADA")
            {
                return ShippingZone.National;
            }
            
            // All other Egyptian cities are national delivery
            return ShippingZone.National;
        }

        private bool IsSimpleSameDayEligible(string city, string country)
        {
            if (string.IsNullOrWhiteSpace(country) || country.ToUpperInvariant() != "EG")
                return false;

            if (string.IsNullOrWhiteSpace(city))
                return false;

            var cityUpper = city.ToUpperInvariant();
            return cityUpper == "CAIRO";
        }


        private decimal GetSimpleDeliveryFee(ShippingZone zone)
        {
            // Simple delivery fee calculation based on zone (fallback values - all 0 since stores must configure)
            return zone switch
            {
                ShippingZone.SameDay => 0.00m,       // Same-day delivery fee (store must configure)
                ShippingZone.Local => 0.00m,         // Local delivery fee (store must configure)
                ShippingZone.National => 0.00m,      // National delivery fee (store must configure)
                ShippingZone.NotSupported => 0.00m,  // Not supported - no fee
                _ => 0.00m                           // Default delivery fee (store must configure)
            };
        }

        private async Task<decimal> GetStoreDeliveryFeeByZoneAsync(ShippingZone zone, Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get store's shipping configuration
                var config = await _storeShippingConfigurationService.GetConfigurationAsync(storeId, cancellationToken);
                
                return zone switch
                {
                    ShippingZone.SameDay => config.SameDayDeliveryFee,      // Use store's configured fee
                    ShippingZone.Local => config.StandardDeliveryFee,       // Use store's configured fee
                    ShippingZone.National => config.NationalDeliveryFee,   // Use store's configured fee
                    ShippingZone.NotSupported => 0.00m,  // Not supported - no fee
                    _ => 0.00m                          // Default fallback (store must configure)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get store-specific delivery fee for store {StoreId}, using fallback", storeId);
                // Fallback to hardcoded values if store config not found
                return GetSimpleDeliveryFee(zone);
            }
        }


        public async Task<ShippingZone> DetermineStoreShippingZoneAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            _logger.LogDebug("Determining store-specific shipping zone for store: {StoreId}, city: {City}, country: {Country}",
                storeId, city, country);

            try
            {
                // Production-ready approach: Resolve city to governorate and check junction table
                var governorateId = await ResolveGovernorateFromCityAsync(city, country, cancellationToken);

                if (governorateId.HasValue)
                {
                    // Check if store supports this governorate
                    var isSupported = await IsGovernorateSupported(storeId, governorateId.Value, cancellationToken);

                    if (!isSupported)
                    {
                        _logger.LogDebug("Store {StoreId} does not support governorate {GovernorateId}", storeId, governorateId.Value);
                        return ShippingZone.NotSupported;
                    }

                    _logger.LogDebug("Store {StoreId} supports governorate {GovernorateId} for {City}", storeId, governorateId.Value, city);
                }

                // Check store-specific same-day delivery (city-based, legacy support)
                if (await _storeShippingConfigurationService.IsSameDayDeliveryAvailableAsync(storeId, city, cancellationToken))
                {
                    _logger.LogDebug("Store {StoreId} offers same-day delivery to {City}", storeId, city);
                    return ShippingZone.SameDay;
                }

                // Fall back to simple zone determination
                var fallbackZone = GetSimpleFallbackZone(city, country);
                _logger.LogDebug("Using fallback zone {Zone} for store {StoreId} and city {City}", fallbackZone, storeId, city);

                return fallbackZone;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining store shipping zone for store: {StoreId}, city: {City}, country: {Country}",
                    storeId, city, country);

                // Fall back to simple zone determination
                return GetSimpleFallbackZone(city, country);
            }
        }

        public async Task<bool> IsEligibleForStoreSameDayDeliveryAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            _logger.LogDebug("Checking store same-day delivery eligibility for store: {StoreId}, city: {City}", storeId, city);

            try
            {
                // Check store-specific same-day delivery availability
                var isStoreEligible = await _storeShippingConfigurationService.IsSameDayDeliveryAvailableAsync(storeId, city, cancellationToken);
                
                if (isStoreEligible)
                {
                    _logger.LogDebug("Store {StoreId} offers same-day delivery to {City}", storeId, city);
                    return true;
                }

                // Fall back to simple same-day delivery check
                var isFallbackEligible = IsSimpleSameDayEligible(city, country);
                _logger.LogDebug("Fallback same-day delivery eligibility for {City}: {IsEligible}", city, isFallbackEligible);
                
                return isFallbackEligible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking store same-day delivery eligibility for store: {StoreId}, city: {City}", storeId, city);
                return false;
            }
        }


        public async Task<decimal> GetStoreDeliveryFeeAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            _logger.LogDebug("Getting store delivery fee for store: {StoreId}, city: {City}", storeId, city);

            try
            {
                // First determine the shipping zone
                var zone = await DetermineStoreShippingZoneAsync(storeId, city, country, cancellationToken);

                // If zone is not supported, return 0
                if (zone == ShippingZone.NotSupported)
                {
                    _logger.LogDebug("Store {StoreId} does not support delivery to {City}", storeId, city);
                    return 0;
                }

                // Get store-specific delivery fee based on zone
                var storeFee = await GetStoreDeliveryFeeByZoneAsync(zone, storeId, cancellationToken);

                _logger.LogDebug("Store {StoreId} delivery fee for {City} (zone: {Zone}): {Fee}", storeId, city, zone, storeFee);
                return storeFee;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store delivery fee for store: {StoreId}, city: {City}", storeId, city);
                throw new InvalidOperationException($"Failed to get delivery fee for store {storeId} to {city}: {ex.Message}", ex);
            }
        }

        public async Task<List<ShippingZone>> GetAvailableDeliveryOptionsAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            _logger.LogDebug("Getting available delivery options for store: {StoreId}, city: {City}", storeId, city);

            try
            {
                var availableOptions = new List<ShippingZone>();

                // Check store-specific same-day delivery
                if (await _storeShippingConfigurationService.IsSameDayDeliveryAvailableAsync(storeId, city, cancellationToken))
                {
                    availableOptions.Add(ShippingZone.SameDay);
                }

                // Get the appropriate zone based on location
                var fallbackZone = GetSimpleFallbackZone(city, country);

                // Add fallback zone if it's not NotSupported and not already in list
                if (fallbackZone != ShippingZone.NotSupported && !availableOptions.Contains(fallbackZone))
                {
                    availableOptions.Add(fallbackZone);
                }

                // If no options available, return NotSupported
                if (availableOptions.Count == 0)
                {
                    availableOptions.Add(ShippingZone.NotSupported);
                }

                _logger.LogDebug("Available delivery options for store {StoreId}, city {City}: {Options}",
                    storeId, city, string.Join(", ", availableOptions));

                return availableOptions;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available delivery options for store: {StoreId}, city: {City}", storeId, city);

                // Fall back to simple zone
                var fallbackZone = GetSimpleFallbackZone(city, country);
                return new List<ShippingZone> { fallbackZone };
            }
        }

        #region Fallback Methods (No Store ID Required)

        /// <summary>
        /// Determines shipping zone using simple fallback logic when store ID is not available
        /// </summary>
        public ShippingZone DetermineShippingZoneFallback(string city, string country)
        {
            _logger.LogDebug("Using fallback shipping zone determination for city: {City}, country: {Country}", 
                city, country);

            return GetSimpleFallbackZone(city, country);
        }

        /// <summary>
        /// Checks if a city is eligible for same-day delivery using simple fallback logic
        /// </summary>
        public bool IsEligibleForSameDayDeliveryFallback(string city, string country)
        {
            _logger.LogDebug("Using fallback same-day delivery check for city: {City}, country: {Country}", 
                city, country);

            return IsSimpleSameDayEligible(city, country);
        }


        /// <summary>
        /// Gets delivery fee using simple fallback logic when store ID is not available
        /// </summary>
        public decimal GetDeliveryFeeFallback(string city, string country)
        {
            _logger.LogDebug("Using fallback delivery fee calculation for city: {City}, country: {Country}", 
                city, country);

            var zone = GetSimpleFallbackZone(city, country);
            return GetSimpleDeliveryFee(zone);
        }

        /// <summary>
        /// Gets delivery fee using store-specific configuration when store ID is available
        /// </summary>
        public async Task<decimal> GetStoreDeliveryFeeFallbackAsync(Guid storeId, string city, string country, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Using store-specific fallback delivery fee calculation for store: {StoreId}, city: {City}, country: {Country}", 
                storeId, city, country);

            var zone = GetSimpleFallbackZone(city, country);
            return await GetStoreDeliveryFeeByZoneAsync(zone, storeId, cancellationToken);
        }

        /// <summary>
        /// Gets available delivery options using simple fallback logic when store ID is not available
        /// </summary>
        public List<ShippingZone> GetAvailableDeliveryOptionsFallback(string city, string country)
        {
            _logger.LogDebug("Using fallback delivery options for city: {City}, country: {Country}", 
                city, country);

            var zone = GetSimpleFallbackZone(city, country);
            return new List<ShippingZone> { zone };
        }

        #endregion

    }
}
