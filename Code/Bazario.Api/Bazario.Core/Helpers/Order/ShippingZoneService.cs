using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Core.Enums;

namespace Bazario.Core.Helpers.Order
{
    /// <summary>
    /// Production-ready shipping zone service that calculates delivery zones based on real address data
    /// </summary>
    public class ShippingZoneService : IShippingZoneService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ShippingZoneService> _logger;
        private readonly Dictionary<string, ShippingZone> _postalCodeZones;
        private readonly Dictionary<string, ShippingZone> _cityZones;
        private readonly Dictionary<string, ShippingZone> _stateZones;
        private readonly Dictionary<string, ShippingZone> _countryZones;

        public ShippingZoneService(IConfiguration configuration, ILogger<ShippingZoneService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize zone mappings from configuration
            _postalCodeZones = LoadPostalCodeZones();
            _cityZones = LoadCityZones();
            _stateZones = LoadStateZones();
            _countryZones = LoadCountryZones();
        }

        public async Task<ShippingZone> DetermineShippingZoneAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Determining shipping zone for address: {City}, {State}, {Country}", 
                    city, state, country);

                // Validate input parameters
                if (string.IsNullOrWhiteSpace(country))
                {
                    _logger.LogWarning("Country is required for shipping zone determination");
                    return GetDefaultZoneForCountry("EG"); // Default to Egypt
                }

                // Check if country is supported
                if (!IsCountrySupported(country))
                {
                    _logger.LogWarning("Country {Country} is not currently supported for shipping. Egypt is the primary market.", country);
                    return GetDefaultZoneForCountry("EG"); // Default to Egypt
                }

                // 1. Check for same-day delivery eligibility
                if (await IsEligibleForSameDayDeliveryAsync(address, city, state, country, postalCode, cancellationToken))
                {
                    _logger.LogDebug("Address eligible for same-day delivery in {Country}", country);
                    return ShippingZone.SameDay;
                }

                // 2. Check for express delivery eligibility
                if (await IsEligibleForExpressDeliveryAsync(address, city, state, country, postalCode, cancellationToken))
                {
                    _logger.LogDebug("Address eligible for express delivery in {Country}", country);
                    return ShippingZone.Express;
                }

                // 3. Check postal code specific zones (if supported by country)
                if (IsPostalCodeSupported(country) && !string.IsNullOrWhiteSpace(postalCode) && _postalCodeZones.TryGetValue(postalCode.ToUpperInvariant(), out var postalZone))
                {
                    _logger.LogDebug("Postal code {PostalCode} mapped to zone {Zone} in {Country}", postalCode, postalZone, country);
                    return postalZone;
                }

                // 4. Check city specific zones
                if (!string.IsNullOrWhiteSpace(city) && _cityZones.TryGetValue(city.ToUpperInvariant(), out var cityZone))
                {
                    _logger.LogDebug("City {City} mapped to zone {Zone} in {Country}", city, cityZone, country);
                    return cityZone;
                }

                // 5. Check state/province zones
                if (!string.IsNullOrWhiteSpace(state) && _stateZones.TryGetValue(state.ToUpperInvariant(), out var stateZone))
                {
                    _logger.LogDebug("State/Province {State} mapped to zone {Zone} in {Country}", state, stateZone, country);
                    return stateZone;
                }

                // 6. Check country zones
                if (_countryZones.TryGetValue(country.ToUpperInvariant(), out var countryZone))
                {
                    _logger.LogDebug("Country {Country} mapped to zone {Zone}", country, countryZone);
                    return countryZone;
                }

                // 7. Default fallback based on country
                var defaultZone = GetDefaultZoneForCountry(country);
                _logger.LogDebug("Using default zone {Zone} for country {Country}", defaultZone, country);
                return defaultZone;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining shipping zone for address: {City}, {State}, {Country}", 
                    city, state, country);
                return GetDefaultZoneForCountry("EG"); // Safe fallback to Egypt
            }
        }

        public decimal GetZoneMultiplier(ShippingZone zone)
        {
            // Delivery time multipliers optimized for Egypt but scalable for future expansion
            return zone switch
            {
                ShippingZone.SameDay => 0.3m,      // 3-4 hours for same-day delivery (major cities)
                ShippingZone.Express => 0.6m,       // 6-8 hours for express delivery (major cities)
                ShippingZone.Local => 1.0m,         // 12-24 hours for local delivery
                ShippingZone.Regional => 1.5m,      // 18-36 hours for regional delivery
                ShippingZone.National => 2.0m,      // 24-48 hours for national delivery
                ShippingZone.International => 4.0m, // 96+ hours for international delivery (future expansion)
                ShippingZone.Remote => 3.0m,        // 36-72 hours for remote areas
                _ => 1.0m                           // Default fallback
            };
        }

        public int GetEstimatedDeliveryHours(ShippingZone zone)
        {
            // Delivery time estimates optimized for Egypt but scalable for future expansion
            return zone switch
            {
                ShippingZone.SameDay => 4,          // Same day delivery (major cities)
                ShippingZone.Express => 8,          // Express delivery (major cities)
                ShippingZone.Local => 24,           // 1 day delivery (same city)
                ShippingZone.Regional => 36,        // 1.5 day delivery (same region)
                ShippingZone.National => 48,        // 2 day delivery (different region)
                ShippingZone.International => 168,  // 7 day delivery (international - future expansion)
                ShippingZone.Remote => 72,          // 3 day delivery (remote areas)
                _ => 24                             // Default 1 day delivery
            };
        }

        public async Task<bool> IsEligibleForExpressDeliveryAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if country supports express delivery
                if (string.IsNullOrWhiteSpace(country) || !IsCountrySupported(country))
                    return false;

                if (string.IsNullOrWhiteSpace(city))
                    return false;

                // Check if city is in express delivery zone for the specific country
                var expressCities = GetExpressDeliveryCitiesForCountry(country);
                var isEligible = expressCities.Contains(city.ToUpperInvariant());

                _logger.LogDebug("Express delivery eligibility for {City}, {Country}: {IsEligible}", city, country, isEligible);
                
                await Task.CompletedTask; // Placeholder for async operation
                return isEligible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking express delivery eligibility for {Country}", country);
                return false;
            }
        }

        public async Task<bool> IsEligibleForSameDayDeliveryAsync(string address, string city, string state, string country, string postalCode, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if country supports same-day delivery
                if (string.IsNullOrWhiteSpace(country) || !IsCountrySupported(country))
                    return false;

                if (string.IsNullOrWhiteSpace(city))
                    return false;

                // Check if city is in same-day delivery zone for the specific country
                var sameDayCities = GetSameDayDeliveryCitiesForCountry(country);
                var isEligible = sameDayCities.Contains(city.ToUpperInvariant());

                _logger.LogDebug("Same-day delivery eligibility for {City}, {Country}: {IsEligible}", city, country, isEligible);
                
                await Task.CompletedTask; // Placeholder for async operation
                return isEligible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking same-day delivery eligibility for {Country}", country);
                return false;
            }
        }

        private Dictionary<string, ShippingZone> LoadPostalCodeZones()
        {
            var zones = new Dictionary<string, ShippingZone>();
            
            var postalCodeConfig = _configuration.GetSection("Shipping:PostalCodeZones");
            foreach (var section in postalCodeConfig.GetChildren())
            {
                if (Enum.TryParse<ShippingZone>(section.Value, out var zone))
                {
                    zones[section.Key.ToUpperInvariant()] = zone;
                }
            }

            // Currently no postal code zones configured (Egypt doesn't use them)
            // Future countries can be added here
            if (!zones.Any())
            {
                _logger.LogDebug("No postal code zones configured - using city/state based zones");
            }

            return zones;
        }

        private Dictionary<string, ShippingZone> LoadCityZones()
        {
            var zones = new Dictionary<string, ShippingZone>();
            
            var cityConfig = _configuration.GetSection("Shipping:CityZones");
            foreach (var section in cityConfig.GetChildren())
            {
                if (Enum.TryParse<ShippingZone>(section.Value, out var zone))
                {
                    zones[section.Key.ToUpperInvariant()] = zone;
                }
            }

            // Default Egyptian city zones if none configured
            if (!zones.Any())
            {
                // Same-day delivery cities (Cairo and Alexandria)
                zones["CAIRO"] = ShippingZone.SameDay;
                zones["ALEXANDRIA"] = ShippingZone.SameDay;
                
                // Express delivery cities (Major Egyptian cities)
                zones["GIZA"] = ShippingZone.Express;
                zones["SHUBRA EL KHEIMA"] = ShippingZone.Express;
                zones["PORT SAID"] = ShippingZone.Express;
                zones["SUEZ"] = ShippingZone.Express;
                zones["LUXOR"] = ShippingZone.Express;
                zones["ASWAN"] = ShippingZone.Express;
                zones["ISMAILIA"] = ShippingZone.Express;
                zones["FAYYUM"] = ShippingZone.Express;
                zones["ZAGAZIG"] = ShippingZone.Express;
                zones["ASUIT"] = ShippingZone.Express;
                zones["TANTA"] = ShippingZone.Express;
                zones["MANSOURA"] = ShippingZone.Express;
                zones["DAMANHUR"] = ShippingZone.Express;
                zones["MINYA"] = ShippingZone.Express;
                zones["BENI SUEF"] = ShippingZone.Express;
                zones["QENA"] = ShippingZone.Express;
                zones["SOHAAG"] = ShippingZone.Express;
                zones["HURGHADA"] = ShippingZone.Express;
                zones["SHARM EL SHEIKH"] = ShippingZone.Express;
            }

            return zones;
        }

        private Dictionary<string, ShippingZone> LoadStateZones()
        {
            var zones = new Dictionary<string, ShippingZone>();
            
            var stateConfig = _configuration.GetSection("Shipping:StateZones");
            foreach (var section in stateConfig.GetChildren())
            {
                if (Enum.TryParse<ShippingZone>(section.Value, out var zone))
                {
                    zones[section.Key.ToUpperInvariant()] = zone;
                }
            }

            // Default Egyptian governorate zones if none configured
            if (!zones.Any())
            {
                // Major governorates with express delivery
                zones["CAIRO"] = ShippingZone.Express;
                zones["ALEXANDRIA"] = ShippingZone.Express;
                zones["GIZA"] = ShippingZone.Express;
                zones["QALYUBIA"] = ShippingZone.Express;
                zones["PORT SAID"] = ShippingZone.Express;
                zones["SUEZ"] = ShippingZone.Express;
                zones["ISMAILIA"] = ShippingZone.Express;
                zones["LUXOR"] = ShippingZone.Express;
                zones["ASWAN"] = ShippingZone.Express;
                zones["HURGHADA"] = ShippingZone.Express;
                zones["SHARM EL SHEIKH"] = ShippingZone.Express;
                
                // Regional governorates
                zones["DAKAHLIA"] = ShippingZone.Regional;
                zones["SHARQIA"] = ShippingZone.Regional;
                zones["KAFR EL SHEIKH"] = ShippingZone.Regional;
                zones["GHARBIA"] = ShippingZone.Regional;
                zones["MONUFIA"] = ShippingZone.Regional;
                zones["BEHEIRA"] = ShippingZone.Regional;
                zones["FAYYUM"] = ShippingZone.Regional;
                zones["BENI SUEF"] = ShippingZone.Regional;
                zones["MINYA"] = ShippingZone.Regional;
                zones["ASUIT"] = ShippingZone.Regional;
                zones["SOHAAG"] = ShippingZone.Regional;
                zones["QENA"] = ShippingZone.Regional;
                zones["RED SEA"] = ShippingZone.Regional;
                zones["NORTH SINAI"] = ShippingZone.Regional;
                zones["SOUTH SINAI"] = ShippingZone.Regional;
                zones["NEW VALLEY"] = ShippingZone.Remote;
                zones["MATROUH"] = ShippingZone.Remote;
            }

            return zones;
        }

        private Dictionary<string, ShippingZone> LoadCountryZones()
        {
            var zones = new Dictionary<string, ShippingZone>();
            
            var countryConfig = _configuration.GetSection("Shipping:CountryZones");
            foreach (var section in countryConfig.GetChildren())
            {
                if (Enum.TryParse<ShippingZone>(section.Value, out var zone))
                {
                    zones[section.Key.ToUpperInvariant()] = zone;
                }
            }

            // Default country zones if none configured
            if (!zones.Any())
            {
                // Egypt (primary market)
                zones["EG"] = ShippingZone.Local;
                zones["EGYPT"] = ShippingZone.Local;
                zones["EGY"] = ShippingZone.Local;
                
                // Future expansion countries can be added here
                // zones["US"] = ShippingZone.National;
                // zones["CA"] = ShippingZone.National;
                // zones["GB"] = ShippingZone.International;
            }

            return zones;
        }

        private ShippingZone GetDefaultZoneForCountry(string country)
        {
            // Default zones based on country
            return country?.ToUpperInvariant() switch
            {
                "EG" or "EGYPT" or "EGY" => ShippingZone.Local,        // Egypt - local delivery
                "US" or "CA" => ShippingZone.National,                 // North America - national
                "GB" or "UK" or "DE" or "FR" or "IT" or "ES" => ShippingZone.International, // Europe - international
                _ => ShippingZone.Local                                // Default to local
            };
        }

        private bool IsCountrySupported(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return false;

            var countryUpper = country.ToUpperInvariant();
            
            // Currently supported countries
            var supportedCountries = new HashSet<string>
            {
                "EG", "EGYPT", "EGY"  // Egypt (primary market)
                // Future countries can be added here
                // "US", "CA", "GB", "UK", "DE", "FR", "IT", "ES"
            };

            return supportedCountries.Contains(countryUpper);
        }

        private bool IsPostalCodeSupported(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return false;

            var countryUpper = country.ToUpperInvariant();
            
            // Countries that use postal codes for shipping zones
            var postalCodeCountries = new HashSet<string>
            {
                // "US", "CA", "GB", "UK", "DE", "FR", "IT", "ES"  // Future expansion
            };

            return postalCodeCountries.Contains(countryUpper);
        }

        private HashSet<string> GetExpressDeliveryCitiesForCountry(string country)
        {
            var countryUpper = country?.ToUpperInvariant();
            
            return countryUpper switch
            {
                "EG" or "EGYPT" or "EGY" => GetEgyptianExpressDeliveryCities(),
                // Future countries can be added here
                // "US" => GetUSExpressDeliveryCities(),
                // "CA" => GetCanadaExpressDeliveryCities(),
                // "GB" or "UK" => GetUKExpressDeliveryCities(),
                _ => new HashSet<string>() // No express cities for unsupported countries
            };
        }

        private HashSet<string> GetSameDayDeliveryCitiesForCountry(string country)
        {
            var countryUpper = country?.ToUpperInvariant();
            
            return countryUpper switch
            {
                "EG" or "EGYPT" or "EGY" => GetEgyptianSameDayDeliveryCities(),
                // Future countries can be added here
                // "US" => GetUSSameDayDeliveryCities(),
                // "CA" => GetCanadaSameDayDeliveryCities(),
                // "GB" or "UK" => GetUKSameDayDeliveryCities(),
                _ => new HashSet<string>() // No same-day cities for unsupported countries
            };
        }

        private HashSet<string> GetEgyptianExpressDeliveryCities()
        {
            return new HashSet<string>
            {
                "CAIRO", "ALEXANDRIA", "GIZA", "SHUBRA EL KHEIMA", "PORT SAID", "SUEZ",
                "LUXOR", "ASWAN", "ISMAILIA", "FAYYUM", "ZAGAZIG", "ASUIT", "TANTA",
                "MANSOURA", "DAMANHUR", "MINYA", "BENI SUEF", "QENA", "SOHAAG",
                "HURGHADA", "SHARM EL SHEIKH"
            };
        }

        private HashSet<string> GetEgyptianSameDayDeliveryCities()
        {
            return new HashSet<string>
            {
                "CAIRO", "ALEXANDRIA"
            };
        }
    }
}
