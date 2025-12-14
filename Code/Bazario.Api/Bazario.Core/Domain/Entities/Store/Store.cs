using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities.Store
{
    public class Store
    {
        [Key]
        public Guid StoreId { get; set; }

        [ForeignKey(nameof(Seller))]
        public Guid SellerId { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? Logo { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        // ---------- Store Status Properties ----------

        /// <summary>
        /// Indicates if the store is active and available for business
        /// </summary>
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        // ---------- Soft Deletion Properties ----------

        /// <summary>
        /// Indicates if the store has been soft deleted
        /// </summary>
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the store was soft deleted
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// ID of the user who deleted the store (for audit trail)
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Reason provided for deleting the store
        /// </summary>
        [StringLength(500)]
        public string? DeletedReason { get; set; }

        // ---------- Concurrency Control ----------
        /// <summary>
        /// Row version for optimistic concurrency control
        /// Automatically managed by EF Core to prevent lost updates during concurrent store modifications
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ---------- Navigation Properties ----------

        public ApplicationUser? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }

        /// <summary>
        /// Store's shipping configuration (1:1 relationship)
        /// </summary>
        public StoreShippingConfiguration? ShippingConfiguration { get; set; }

        /// <summary>
        /// Governorates that this store supports or excludes for shipping (1:N relationship)
        /// </summary>
        public ICollection<StoreGovernorateSupport> GovernorateSupports { get; set; } = new List<StoreGovernorateSupport>();

    }
}
