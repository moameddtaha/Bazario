using System;
using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.DTO.Location.Governorate
{
    /// <summary>
    /// Request model for updating an existing governorate/state
    /// </summary>
    public class GovernorateUpdateRequest
    {
        [Required(ErrorMessage = "Governorate ID is required")]
        public Guid GovernorateId { get; set; }

        [Required(ErrorMessage = "Governorate name is required")]
        [MaxLength(100, ErrorMessage = "Governorate name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Arabic name cannot exceed 100 characters")]
        public string? NameArabic { get; set; }

        [MaxLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public bool SupportsSameDayDelivery { get; set; } = false;
    }
}
