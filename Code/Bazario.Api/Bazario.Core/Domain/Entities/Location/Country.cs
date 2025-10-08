using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.Domain.Entities.Location
{
    /// <summary>
    /// Country entity for managing shipping locations
    /// </summary>
    public class Country
    {
        public Guid CountryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty; // ISO 3166-1 alpha-2 code (e.g., "EG", "SA", "AE")

        [MaxLength(100)]
        public string? NameArabic { get; set; } // Arabic name for localization

        public bool IsActive { get; set; } = true;

        public bool SupportsPostalCodes { get; set; } = false; // Does this country use postal codes for shipping?

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
    }
}
