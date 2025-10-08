using System;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.DTO.Location.City
{
    /// <summary>
    /// Request model for updating an existing city
    /// </summary>
    public class CityUpdateRequest
    {
        [Required(ErrorMessage = "City ID is required")]
        public Guid CityId { get; set; }

        [Required(ErrorMessage = "City name is required")]
        [MaxLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Arabic name cannot exceed 100 characters")]
        public string? NameArabic { get; set; }

        [MaxLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public bool SupportsSameDayDelivery { get; set; } = false;
    }
}
