using System;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.DTO.Location.Country
{
    /// <summary>
    /// Request model for updating an existing country
    /// </summary>
    public class CountryUpdateRequest
    {
        [Required(ErrorMessage = "Country ID is required")]
        public Guid CountryId { get; set; }

        [Required(ErrorMessage = "Country name is required")]
        [MaxLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Arabic name cannot exceed 100 characters")]
        public string? NameArabic { get; set; }

        public bool IsActive { get; set; } = true;

        public bool SupportsPostalCodes { get; set; } = false;
    }
}
