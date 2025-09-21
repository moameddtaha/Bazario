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

        public OrderEntity ToOrder()
        {
            return new OrderEntity
            {
                OrderId = OrderId,
                Date = Date ?? DateTime.UtcNow, // Provide current date as default
                TotalAmount = TotalAmount ?? 0, // Only provide default for required field
                Status = Status?.ToString()
            };
        }
    }
}
