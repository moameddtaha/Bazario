using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Domain.IdentityEntities;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;

namespace Bazario.Core.Domain.Entities.Inventory
{
    /// <summary>
    /// Domain entity representing a stock reservation for order processing
    /// </summary>
    public class StockReservation
    {
        [Key]
        public Guid ReservationId { get; set; }

        [ForeignKey(nameof(Product))]
        public Guid ProductId { get; set; }

        [Required]
        public int ReservedQuantity { get; set; }

        [ForeignKey(nameof(Customer))]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Optional reference to the order if reservation is linked to an order
        /// </summary>
        [ForeignKey(nameof(Order))]
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Optional external reference identifier (e.g., cart ID, order reference)
        /// </summary>
        [StringLength(100)]
        public string? ExternalReference { get; set; }

        /// <summary>
        /// Reservation status: Pending, Confirmed, Released, Expired
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Date when the reservation was confirmed (converted to order)
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// Date when the reservation was released or expired
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? ReleasedAt { get; set; }

        // ---------- Soft Deletion Properties ----------

        /// <summary>
        /// Indicates if the reservation has been soft deleted
        /// </summary>
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Date and time when the reservation was soft deleted (UTC)
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User ID who performed the soft deletion
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Reason for soft deletion
        /// </summary>
        [StringLength(500)]
        public string? DeletedReason { get; set; }

        // ---------- Navigation Properties ----------

        public Product? Product { get; set; }

        public ApplicationUser? Customer { get; set; }

        public OrderEntity? Order { get; set; }
    }
}
