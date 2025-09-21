using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums;

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

        public override bool Equals(object? obj)
        {
            return obj is OrderResponse response &&
                   OrderId.Equals(response.OrderId) &&
                   CustomerId.Equals(response.CustomerId) &&
                   Date == response.Date &&
                   TotalAmount == response.TotalAmount &&
                   Status == response.Status;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(OrderId);
            hash.Add(CustomerId);
            hash.Add(Date);
            hash.Add(TotalAmount);
            hash.Add(Status);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Order: ID: {OrderId}, Customer ID: {CustomerId}, Date: {Date:yyyy-MM-dd HH:mm:ss}, Total Amount: {TotalAmount:C}, Status: {Status}";
        }

        public OrderUpdateRequest ToOrderUpdateRequest()
        {
            return new OrderUpdateRequest()
            {
                OrderId = OrderId,
                Date = Date,
                TotalAmount = TotalAmount,
                Status = Status
            };
        }
    }
}
