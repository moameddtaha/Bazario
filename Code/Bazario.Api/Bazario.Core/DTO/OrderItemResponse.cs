using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Core.DTO
{
    public class OrderItemResponse
    {
        public Guid OrderItemId { get; set; }

        [Display(Name = "Order Id")]
        public Guid OrderId { get; set; }

        [Display(Name = "Product Id")]
        public Guid ProductId { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Subtotal")]
        public decimal Subtotal => Quantity * Price;

        public override bool Equals(object? obj)
        {
            return obj is OrderItemResponse response &&
                   OrderItemId.Equals(response.OrderItemId) &&
                   OrderId.Equals(response.OrderId) &&
                   ProductId.Equals(response.ProductId) &&
                   Quantity == response.Quantity &&
                   Price == response.Price;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(OrderItemId);
            hash.Add(OrderId);
            hash.Add(ProductId);
            hash.Add(Quantity);
            hash.Add(Price);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"OrderItem: ID: {OrderItemId}, Order ID: {OrderId}, Product ID: {ProductId}, Quantity: {Quantity}, Price: {Price:C}, Subtotal: {Subtotal:C}";
        }

        public OrderItemUpdateRequest ToOrderItemUpdateRequest()
        {
            return new OrderItemUpdateRequest()
            {
                OrderItemId = OrderItemId,
                Quantity = Quantity,
                Price = Price
            };
        }
    }
}
