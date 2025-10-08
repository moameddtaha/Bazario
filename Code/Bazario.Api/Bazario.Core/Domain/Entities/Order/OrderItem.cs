using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bazario.Core.Domain.Entities.Catalog;

namespace Bazario.Core.Domain.Entities.Order
{
    public class OrderItem
    {
        [Key]
        public Guid OrderItemId { get; set; }

        [ForeignKey(nameof(Order))]
        public Guid OrderId { get; set; }

        [ForeignKey(nameof(Product))]
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        // ---------- Navigation Properties ----------

        public Order? Order { get; set; }

        public Product? Product { get; set; }
    }
}