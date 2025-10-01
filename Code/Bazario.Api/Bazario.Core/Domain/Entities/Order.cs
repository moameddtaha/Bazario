using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }

        [ForeignKey(nameof(Customer))]
        public Guid CustomerId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? Status { get; set; }

        /// <summary>
        /// Discount codes applied to this order (comma-separated for storage)
        /// </summary>
        [StringLength(500)]
        public string? AppliedDiscountCodes { get; set; }

        /// <summary>
        /// Total amount of discounts applied to this order
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>
        /// Types of discounts applied (comma-separated for storage)
        /// </summary>
        [StringLength(100)]
        public string? AppliedDiscountTypes { get; set; }

        /// <summary>
        /// Shipping cost for this order
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; } = 0;

        /// <summary>
        /// Subtotal before discounts and shipping
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        // ---------- Navigation Properties ----------

        public ApplicationUser? Customer { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
