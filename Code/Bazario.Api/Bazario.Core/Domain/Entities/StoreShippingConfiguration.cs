using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Bazario.Core.Domain.Entities
{
    /// <summary>
    /// Store shipping configuration entity
    /// </summary>
    public class StoreShippingConfiguration
    {
        public Guid ConfigurationId { get; set; }
        public Guid StoreId { get; set; }
        public string DefaultShippingZone { get; set; } = string.Empty;
        public bool OffersSameDayDelivery { get; set; } = false;
        public bool OffersStandardDelivery { get; set; } = true;
        public int? SameDayCutoffHour { get; set; }
        [MaxLength(500)]
        public string? ShippingNotes { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SameDayDeliveryFee { get; set; } = 0m;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal StandardDeliveryFee { get; set; } = 0m;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal NationalDeliveryFee { get; set; } = 0m;
        
        public string SupportedCities { get; set; } = string.Empty; // Comma-separated list of supported cities
        public string ExcludedCities { get; set; } = string.Empty; // Comma-separated list of excluded cities
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Store Store { get; set; } = null!;

        // Helper properties for comma-separated string handling
        public List<string> SupportedCitiesList
        {
            get => string.IsNullOrEmpty(SupportedCities) 
                ? new List<string>() 
                : SupportedCities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToUpperInvariant())
                    .ToList();
            set => SupportedCities = value == null || !value.Any() 
                ? string.Empty 
                : string.Join(",", value.Select(c => c.Trim().ToUpperInvariant()));
        }

        public List<string> ExcludedCitiesList
        {
            get => string.IsNullOrEmpty(ExcludedCities) 
                ? new List<string>() 
                : ExcludedCities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToUpperInvariant())
                    .ToList();
            set => ExcludedCities = value == null || !value.Any() 
                ? string.Empty 
                : string.Join(",", value.Select(c => c.Trim().ToUpperInvariant()));
        }
    }
}
