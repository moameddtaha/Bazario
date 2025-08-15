using System.ComponentModel.DataAnnotations;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }

        [ForeignKey(nameof(Customer))]
        public Guid CustomerId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Status { get; set; }

        // ---------- Navigation Properties ----------

        public ApplicationUser? Customer { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
