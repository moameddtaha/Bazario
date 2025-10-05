using Bazario.Core.Enums;

namespace Bazario.Core.DTO.Store
{
    /// <summary>
    /// Response model for store shipping configuration
    /// </summary>
    public class StoreShippingConfigurationResponse
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public ShippingZone DefaultShippingZone { get; set; }
        public bool OffersSameDayDelivery { get; set; }
        public bool OffersStandardDelivery { get; set; }
        public int? SameDayCutoffHour { get; set; }
        public string? ShippingNotes { get; set; }
        public decimal SameDayDeliveryFee { get; set; }
        public decimal StandardDeliveryFee { get; set; }
        public decimal NationalDeliveryFee { get; set; }
        public List<string> SupportedCities { get; set; } = new();
        public List<string> ExcludedCities { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
