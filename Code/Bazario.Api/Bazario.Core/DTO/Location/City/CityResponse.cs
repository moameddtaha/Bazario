using System;

namespace Bazario.Core.DTO.Location.City
{
    /// <summary>
    /// Response model for city data
    /// </summary>
    public class CityResponse
    {
        public Guid CityId { get; set; }
        public Guid GovernorateId { get; set; }
        public string GovernorateName { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameArabic { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public bool SupportsSameDayDelivery { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
