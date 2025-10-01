using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities
{
    /// <summary>
    /// Discount codes and promotions that can be applied to orders
    /// Supports both store-specific and global discounts
    /// </summary>
    public class Discount
    {
        [Key]
        public Guid DiscountId { get; set; }

        /// <summary>
        /// Unique discount code that customers enter (e.g., "SAVE10", "WELCOME50")
        /// </summary>
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Type of discount (Percentage or FixedAmount)
        /// </summary>
        public DiscountType Type { get; set; }

        /// <summary>
        /// Discount value (percentage as decimal 0.10 = 10%, or fixed amount in EGP)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        /// <summary>
        /// When the discount becomes valid
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// When the discount expires
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Minimum order amount required to use this discount (0 = no minimum)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrderAmount { get; set; } = 0;

        /// <summary>
        /// Whether this discount code has been used (one-time use only)
        /// </summary>
        [DefaultValue(false)]
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Store ID this discount applies to (null = global discount for all stores)
        /// </summary>
        [ForeignKey(nameof(Store))]
        public Guid? ApplicableStoreId { get; set; }

        /// <summary>
        /// Whether this discount is currently active
        /// </summary>
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Description of the discount for admin reference
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When this discount was created
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this discount was last updated
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID of the user who created this discount
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// ID of the user who last updated this discount
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        // ---------- Navigation Properties ----------

        /// <summary>
        /// The store this discount applies to (null for global discounts)
        /// </summary>
        public Store? Store { get; set; }
    }
}
