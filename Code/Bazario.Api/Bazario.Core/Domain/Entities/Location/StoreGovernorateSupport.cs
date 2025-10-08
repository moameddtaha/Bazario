using System;
using System.ComponentModel.DataAnnotations;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;

namespace Bazario.Core.Domain.Entities.Location
{
    /// <summary>
    /// Junction table for many-to-many relationship between stores and governorates
    /// Tracks which governorates a store supports or excludes for shipping
    /// </summary>
    public class StoreGovernorateSupport
    {
        [Key]
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        public Guid GovernorateId { get; set; }

        /// <summary>
        /// True = store supports shipping to this governorate
        /// False = store explicitly excludes this governorate
        /// </summary>
        public bool IsSupported { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public StoreEntity Store { get; set; } = null!;

        public Governorate Governorate { get; set; } = null!;
    }
}