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
    public class OrderAddRequest
    {
        [Required(ErrorMessage = "Customer Id cannot be blank")]
        [Display(Name = "Customer Id")]
        public Guid CustomerId { get; set; }

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

        public OrderEntity ToOrder()
        {
            return new OrderEntity
            {
                CustomerId = CustomerId,
                Date = Date,
                TotalAmount = TotalAmount,
                Status = Status.ToString()
            };
        }
    }
}
