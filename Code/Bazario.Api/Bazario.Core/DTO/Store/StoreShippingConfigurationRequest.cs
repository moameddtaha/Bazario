using System.ComponentModel.DataAnnotations;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.DTO.Store
{
    /// <summary>
    /// Request model for store shipping configuration
    /// </summary>
    public class StoreShippingConfigurationRequest
    {
        [Required(ErrorMessage = "Store ID is required")]
        public Guid StoreId { get; set; }

        [Required(ErrorMessage = "Shipping zone is required")]
        public ShippingZone DefaultShippingZone { get; set; }

        [Required(ErrorMessage = "Same-day delivery availability is required")]
        public bool OffersSameDayDelivery { get; set; }

        [Required(ErrorMessage = "Standard delivery availability is required")]
        public bool OffersStandardDelivery { get; set; } = true;

        [Range(0, 23, ErrorMessage = "Same-day cutoff hour must be between 0 and 23")]
        public int? SameDayCutoffHour { get; set; } // Hour of day when same-day orders must be placed by


        [StringLength(500, ErrorMessage = "Shipping notes cannot exceed 500 characters")]
        public string? ShippingNotes { get; set; }

        [Required(ErrorMessage = "Same-day delivery fee is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Same-day delivery fee must be 0 or greater")]
        public decimal SameDayDeliveryFee { get; set; } = 0m;

        [Required(ErrorMessage = "Standard delivery fee is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Standard delivery fee must be 0 or greater")]
        public decimal StandardDeliveryFee { get; set; } = 0m;

        [Required(ErrorMessage = "National delivery fee is required")]
        [Range(0, double.MaxValue, ErrorMessage = "National delivery fee must be 0 or greater")]
        public decimal NationalDeliveryFee { get; set; } = 0m;

        // Governorate-based shipping configuration
        public List<Guid>? SupportedGovernorateIds { get; set; } // Governorates where this store delivers
        public List<Guid>? ExcludedGovernorateIds { get; set; } // Governorates where this store does NOT deliver
    }
}
