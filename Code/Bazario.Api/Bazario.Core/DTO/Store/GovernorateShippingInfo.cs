using System;

namespace Bazario.Core.DTO.Store
{
    /// <summary>
    /// Governorate information for shipping configuration responses
    /// </summary>
    public class GovernorateShippingInfo
    {
        public Guid GovernorateId { get; set; }
        public string GovernorateName { get; set; } = string.Empty;
        public string? GovernorateNameArabic { get; set; }
        public Guid CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public bool SupportsSameDayDelivery { get; set; }
    }
}