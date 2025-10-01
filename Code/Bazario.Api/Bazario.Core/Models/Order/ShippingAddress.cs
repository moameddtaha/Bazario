using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Shipping address information for order delivery
    /// </summary>
    public class ShippingAddress
    {
        /// <summary>
        /// Full shipping address (street address, building number, etc.)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// City for delivery
        /// </summary>
        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State/Province/Governorate for delivery
        /// </summary>
        [StringLength(100)]
        public string? State { get; set; }

        /// <summary>
        /// Postal code (optional for Egypt, but useful for future expansion)
        /// </summary>
        [StringLength(20)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Additional delivery instructions
        /// </summary>
        [StringLength(500)]
        public string? DeliveryInstructions { get; set; }

        /// <summary>
        /// Contact phone number for delivery
        /// </summary>
        [StringLength(20)]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Contact name for delivery
        /// </summary>
        [StringLength(100)]
        public string? ContactName { get; set; }
    }
}
