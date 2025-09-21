using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;

namespace Bazario.Core.Extensions
{
    public static class OrderExtensions
    {
        public static OrderResponse ToOrderResponse(this Order order)
        {
            return new OrderResponse
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                Date = order.Date,
                TotalAmount = order.TotalAmount,
                Status = Enum.TryParse<OrderStatus>(order.Status, out var status) ? status : OrderStatus.Pending
            };
        }
    }
}
