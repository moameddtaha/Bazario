using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.DTO.Order
{
    public class OrderResponse
    {
        public Guid OrderId { get; set; }

        [Display(Name = "Customer Id")]
        public Guid CustomerId { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Order Status")]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Discount codes applied to this order (comma-separated)
        /// </summary>
        [Display(Name = "Applied Discount Codes")]
        public string? AppliedDiscountCodes { get; set; }

        /// <summary>
        /// Total amount of discounts applied to this order
        /// </summary>
        [Display(Name = "Discount Amount")]
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>
        /// Types of discounts applied (comma-separated)
        /// </summary>
        [Display(Name = "Applied Discount Types")]
        public string? AppliedDiscountTypes { get; set; }

        /// <summary>
        /// Shipping cost for this order
        /// </summary>
        [Display(Name = "Shipping Cost")]
        public decimal ShippingCost { get; set; } = 0;

        /// <summary>
        /// Subtotal before discounts and shipping
        /// </summary>
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// </summary>
        public byte[]? RowVersion { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is OrderResponse response &&
                   OrderId.Equals(response.OrderId) &&
                   CustomerId.Equals(response.CustomerId) &&
                   Date == response.Date &&
                   TotalAmount == response.TotalAmount &&
                   Status == response.Status &&
                   AppliedDiscountCodes == response.AppliedDiscountCodes &&
                   DiscountAmount == response.DiscountAmount &&
                   AppliedDiscountTypes == response.AppliedDiscountTypes &&
                   ShippingCost == response.ShippingCost &&
                   Subtotal == response.Subtotal;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(OrderId);
            hash.Add(CustomerId);
            hash.Add(Date);
            hash.Add(TotalAmount);
            hash.Add(Status);
            hash.Add(AppliedDiscountCodes);
            hash.Add(DiscountAmount);
            hash.Add(AppliedDiscountTypes);
            hash.Add(ShippingCost);
            hash.Add(Subtotal);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Order: ID: {OrderId}, Customer ID: {CustomerId}, Date: {Date:yyyy-MM-dd HH:mm:ss}, Total Amount: {TotalAmount:C}, Status: {Status}, Subtotal: {Subtotal:C}, Discount: {DiscountAmount:C}, Shipping: {ShippingCost:C}";
        }

        public OrderUpdateRequest ToOrderUpdateRequest()
        {
            return new OrderUpdateRequest()
            {
                OrderId = OrderId,
                Date = Date,
                TotalAmount = TotalAmount,
                Status = Status,
                AppliedDiscountCodes = AppliedDiscountCodes,
                DiscountAmount = DiscountAmount,
                AppliedDiscountTypes = AppliedDiscountTypes,
                ShippingCost = ShippingCost,
                Subtotal = Subtotal,
                RowVersion = RowVersion
            };
        }
    }
}
