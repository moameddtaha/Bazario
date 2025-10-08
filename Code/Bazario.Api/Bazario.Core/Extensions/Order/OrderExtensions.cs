using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.Extensions.Order
{
    public static class OrderExtensions
    {
        public static OrderResponse ToOrderResponse(this OrderEntity order)
        {
            return new OrderResponse
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                Date = order.Date,
                TotalAmount = order.TotalAmount,
                Status = Enum.TryParse<OrderStatus>(order.Status, out var status) ? status : OrderStatus.Pending,
                AppliedDiscountCodes = order.AppliedDiscountCodes,
                DiscountAmount = order.DiscountAmount,
                AppliedDiscountTypes = order.AppliedDiscountTypes,
                ShippingCost = order.ShippingCost,
                Subtotal = order.Subtotal
            };
        }
    }
}
