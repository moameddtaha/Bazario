using System;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.Domain.Entities.Location
{
    /// <summary>
    /// City entity for location-based shipping configuration
    /// </summary>
    public class City
    {
        [Key]
        public Guid CityId { get; set; }

        public Guid GovernorateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NameArabic { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indicates if this city supports same-day delivery (e.g., Cairo downtown)
        /// </summary>
        public bool SupportsSameDayDelivery { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public Governorate Governorate { get; set; } = null!;
    }
}