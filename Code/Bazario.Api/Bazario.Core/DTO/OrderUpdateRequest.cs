using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Enums;

namespace Bazario.Core.DTO
{
    public class OrderUpdateRequest
    {
        [Required(ErrorMessage = "Order Id cannot be blank")]
        public Guid OrderId { get; set; }

        [Required(ErrorMessage = "Date cannot be blank")]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Total Amount cannot be blank")]
        [Display(Name = "Total Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total Amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Status cannot be blank")]
        [Display(Name = "Order Status")]
        public OrderStatus Status { get; set; }

        public Order ToOrder()
        {
            return new Order
            {
                OrderId = OrderId,
                Date = Date,
                TotalAmount = TotalAmount,
                Status = Status.ToString()
            };
        }
    }
}
