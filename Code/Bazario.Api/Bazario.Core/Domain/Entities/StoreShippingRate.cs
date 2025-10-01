using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities
{
    /// <summary>
    /// Store shipping rate configuration for different shipping zones
    /// Allows stores to set different shipping costs based on delivery location
    /// </summary>
    public class StoreShippingRate
    {
        [Key]
        public Guid StoreShippingRateId { get; set; }

        [ForeignKey(nameof(Store))]
        public Guid StoreId { get; set; }

        /// <summary>
        /// The shipping zone this rate applies to
        /// </summary>
        public ShippingZone ShippingZone { get; set; }

        /// <summary>
        /// Flat shipping cost for this zone (in store's currency)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        /// <summary>
        /// Minimum order amount to qualify for free shipping (0 = no free shipping)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal FreeShippingThreshold { get; set; } = 0;

        /// <summary>
        /// Whether this shipping rate is currently active
        /// </summary>
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this shipping rate was created
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this shipping rate was last updated
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID of the user who created this shipping rate
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// ID of the user who last updated this shipping rate
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        // ---------- Navigation Properties ----------

        /// <summary>
        /// The store this shipping rate belongs to
        /// </summary>
        public Store? Store { get; set; }
    }
}
