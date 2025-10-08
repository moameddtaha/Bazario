using System;

namespace Bazario.Core.DTO.Location.Governorate
{
    /// <summary>
    /// Response model for governorate/state data
    /// </summary>
    public class GovernorateResponse
    {
        public Guid GovernorateId { get; set; }
        public Guid CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameArabic { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public bool SupportsSameDayDelivery { get; set; }
        public int CityCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
