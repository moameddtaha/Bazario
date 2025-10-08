using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.DTO.Location.Country
{
    /// <summary>
    /// Request model for creating a new country
    /// </summary>
    public class CountryAddRequest
    {
        [Required(ErrorMessage = "Country name is required")]
        [MaxLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country code is required")]
        [MaxLength(10, ErrorMessage = "Country code cannot exceed 10 characters")]
        [RegularExpression("^[A-Z]{2,3}$", ErrorMessage = "Country code must be 2-3 uppercase letters (ISO standard)")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Arabic name cannot exceed 100 characters")]
        public string? NameArabic { get; set; }

        public bool SupportsPostalCodes { get; set; } = false;
    }
}