using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Enums;
using OrderEntity = Bazario.Core.Domain.Entities.Order;

namespace Bazario.Core.DTO.Order
{
    /// <summary>
    /// Request model for updating an existing order
    /// </summary>
    /// <remarks>
    /// This model allows updating order properties including the new discount and shipping information.
    /// All properties are optional - only provided properties will be updated.
    /// 
    /// Business Rules:
    /// - DiscountAmount cannot exceed Subtotal
    /// - TotalAmount must equal Subtotal + ShippingCost - DiscountAmount
    /// - AppliedDiscountCodes and AppliedDiscountTypes must have matching counts
    /// - If DiscountAmount > 0, AppliedDiscountCodes must be provided
    /// </remarks>
    public class OrderUpdateRequest
    {
        [Required(ErrorMessage = "Order Id cannot be blank")]
        public Guid OrderId { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime? Date { get; set; }

        [Display(Name = "Total Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total Amount must be greater than 0")]
        public decimal? TotalAmount { get; set; }

        [Display(Name = "Order Status")]
        public OrderStatus? Status { get; set; }

        /// <summary>
        /// Discount codes applied to this order (comma-separated)
        /// </summary>
        /// <remarks>
        /// Comma-separated list of discount codes that were applied to this order.
        /// Example: "SAVE10,SUMMER20" for multiple discount codes.
        /// Must match the count of AppliedDiscountTypes if both are provided.
        /// </remarks>
        /// <example>SAVE10,SUMMER20</example>
        [Display(Name = "Applied Discount Codes")]
        [StringLength(500, ErrorMessage = "Applied Discount Codes cannot exceed 500 characters")]
        public string? AppliedDiscountCodes { get; set; }

        /// <summary>
        /// Total amount of discounts applied to this order
        /// </summary>
        /// <remarks>
        /// The total monetary value of all discounts applied to this order.
        /// Must be 0 or greater and cannot exceed the subtotal amount.
        /// If provided, AppliedDiscountCodes must also be provided.
        /// </remarks>
        /// <example>25.50</example>
        [Display(Name = "Discount Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Discount Amount must be 0 or greater")]
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// Types of discounts applied (comma-separated)
        /// </summary>
        /// <remarks>
        /// Comma-separated list of discount types corresponding to the AppliedDiscountCodes.
        /// Valid values: "Percentage" or "FixedAmount".
        /// Must match the count of AppliedDiscountCodes if both are provided.
        /// </remarks>
        /// <example>Percentage,FixedAmount</example>
        [Display(Name = "Applied Discount Types")]
        [StringLength(100, ErrorMessage = "Applied Discount Types cannot exceed 100 characters")]
        public string? AppliedDiscountTypes { get; set; }

        /// <summary>
        /// Shipping cost for this order
        /// </summary>
        /// <remarks>
        /// The cost of shipping for this order.
        /// Must be 0 or greater. Can be 0 for free shipping.
        /// Used in the calculation: Total = Subtotal + ShippingCost - DiscountAmount
        /// </remarks>
        /// <example>15.00</example>
        [Display(Name = "Shipping Cost")]
        [Range(0, double.MaxValue, ErrorMessage = "Shipping Cost must be 0 or greater")]
        public decimal? ShippingCost { get; set; }

        /// <summary>
        /// Subtotal before discounts and shipping
        /// </summary>
        /// <remarks>
        /// The subtotal amount before applying discounts and adding shipping costs.
        /// Must be 0 or greater. This is the sum of all order items before any discounts.
        /// Used in the calculation: Total = Subtotal + ShippingCost - DiscountAmount
        /// </remarks>
        /// <example>150.00</example>
        [Display(Name = "Subtotal")]
        [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be 0 or greater")]
        public decimal? Subtotal { get; set; }

        public OrderEntity ToOrder()
        {
            return new OrderEntity
            {
                OrderId = OrderId,
                Date = Date ?? DateTime.UtcNow, // Provide current date as default
                TotalAmount = TotalAmount ?? 0, // Only provide default for required field
                Status = Status?.ToString(),
                AppliedDiscountCodes = AppliedDiscountCodes,
                DiscountAmount = DiscountAmount ?? 0,
                AppliedDiscountTypes = AppliedDiscountTypes,
                ShippingCost = ShippingCost ?? 0,
                Subtotal = Subtotal ?? 0
            };
        }
    }
}
