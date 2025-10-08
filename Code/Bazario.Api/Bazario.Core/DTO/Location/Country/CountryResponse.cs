using System;

namespace Bazario.Core.DTO.Location.Country
{
    /// <summary>
    /// Response model for country data
    /// </summary>
    public class CountryResponse
    {
        public Guid CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? NameArabic { get; set; }
        public bool IsActive { get; set; }
        public bool SupportsPostalCodes { get; set; }
        public int GovernorateCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
