using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Order;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;

namespace Bazario.Core.DTO.Order
{
    public class OrderAddRequest
    {
        [Required(ErrorMessage = "Customer Id cannot be blank")]
        [Display(Name = "Customer Id")]
        public Guid CustomerId { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime? Date { get; set; } // Optional - will be auto-generated if null

        [Display(Name = "Total Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Total Amount must be 0 or greater")]
        public decimal TotalAmount { get; set; } = 0; // Optional - will be calculated if 0

        [Required(ErrorMessage = "Status cannot be blank")]
        [Display(Name = "Order Status")]
        public OrderStatus Status { get; set; }

        [Required(ErrorMessage = "Order items cannot be empty")]
        [Display(Name = "Order Items")]
        [MinLength(1, ErrorMessage = "At least one order item is required")]
        public List<OrderItemAddRequest> OrderItems { get; set; } = new List<OrderItemAddRequest>();

        [Required(ErrorMessage = "Shipping address is required")]
        [Display(Name = "Shipping Address")]
        public ShippingAddress ShippingAddress { get; set; } = new ShippingAddress();

        [Display(Name = "Discount Codes")]
        public List<string> DiscountCodes { get; set; } = new List<string>();

        public OrderEntity ToOrder()
        {
            return new OrderEntity
            {
                CustomerId = CustomerId,
                Date = Date ?? DateTime.UtcNow, // Auto-generate if not provided
                TotalAmount = TotalAmount, // Will be recalculated by service if 0
                Status = Status.ToString(),
                AppliedDiscountCodes = DiscountCodes.Any() ? string.Join(",", DiscountCodes) : null
                // Note: Subtotal, DiscountAmount, AppliedDiscountTypes, ShippingCost 
                // will be populated by the service layer after calculation
            };
        }

        /// <summary>
        /// Determines if the order should be calculated by the service layer
        /// Returns true if Date is null or TotalAmount is 0
        /// </summary>
        public bool ShouldCalculateOrder()
        {
            return Date == null || TotalAmount == 0;
        }
    }
}
