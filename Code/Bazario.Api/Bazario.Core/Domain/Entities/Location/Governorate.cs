using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bazario.Core.Domain.Entities.Location
{
    /// <summary>
    /// Governorate/State entity for managing shipping sub-locations within countries
    /// </summary>
    public class Governorate
    {
        public Guid GovernorateId { get; set; }

        [Required]
        public Guid CountryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NameArabic { get; set; } // Arabic name for localization

        [MaxLength(20)]
        public string? Code { get; set; } // Optional code (e.g., "CAI" for Cairo)

        public bool IsActive { get; set; } = true;

        public bool SupportsSameDayDelivery { get; set; } = false; // Does this governorate support same-day delivery by default?

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CountryId))]
        public Country Country { get; set; } = null!;

        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}
