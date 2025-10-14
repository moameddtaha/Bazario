using System;
using System.Collections.Generic;
using System.Linq;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.DTO.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Helper implementation for store shipping configuration operations
    /// </summary>
    public class StoreShippingConfigurationHelper : IStoreShippingConfigurationHelper
    {
        private readonly ILogger<StoreShippingConfigurationHelper> _logger;

        public StoreShippingConfigurationHelper(ILogger<StoreShippingConfigurationHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Maps a collection of StoreGovernorateSupport entities to GovernorateShippingInfo DTOs
        /// </summary>
        public List<GovernorateShippingInfo> MapGovernorateShippingInfo(IEnumerable<StoreGovernorateSupport> governorates)
        {
            if (governorates == null)
            {
                throw new ArgumentNullException(nameof(governorates));
            }

            return [.. governorates.Select(sg => new GovernorateShippingInfo
            {
                GovernorateId = sg.Governorate.GovernorateId,
                GovernorateName = sg.Governorate.Name,
                GovernorateNameArabic = sg.Governorate.NameArabic,
                CountryId = sg.Governorate.CountryId,
                CountryName = sg.Governorate.Country.Name,
                SupportsSameDayDelivery = sg.Governorate.SupportsSameDayDelivery
            })];
        }

        /// <summary>
        /// Validates the shipping configuration request for business rules
        /// </summary>
        public void ValidateConfigurationBusinessRules(StoreShippingConfigurationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Validate no conflicting governorates (can't be both supported AND excluded)
            if (request.SupportedGovernorateIds != null && request.ExcludedGovernorateIds != null)
            {
                var conflicts = request.SupportedGovernorateIds.Intersect(request.ExcludedGovernorateIds).ToList();
                if (conflicts.Count > 0)
                {
                    var conflictIds = string.Join(", ", conflicts);
                    throw new ArgumentException($"Governorates cannot be both supported and excluded. Conflicting IDs: {conflictIds}");
                }
            }

            // Validate same-day delivery requires cutoff hour
            if (request.OffersSameDayDelivery && !request.SameDayCutoffHour.HasValue)
            {
                throw new ArgumentException("Same-day delivery requires a cutoff hour to be specified");
            }

            // Validate same-day delivery requires at least some supported governorates
            if (request.OffersSameDayDelivery &&
                (request.SupportedGovernorateIds == null || request.SupportedGovernorateIds.Count == 0))
            {
                throw new ArgumentException("Same-day delivery requires at least one supported governorate");
            }

            // Validate pricing logic - same-day should be >= standard (warning via log)
            if (request.OffersSameDayDelivery &&
                request.SameDayDeliveryFee < request.StandardDeliveryFee)
            {
                _logger.LogWarning("Same-day delivery fee ({SameDayFee}) is less than standard delivery fee ({StandardFee}). This is unusual pricing.",
                    request.SameDayDeliveryFee, request.StandardDeliveryFee);
            }

            // Validate at least one delivery option is offered
            if (!request.OffersSameDayDelivery && !request.OffersStandardDelivery)
            {
                throw new ArgumentException("At least one delivery option (same-day or standard) must be offered");
            }
        }
    }
}
