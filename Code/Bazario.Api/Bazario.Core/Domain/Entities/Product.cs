using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities
{
    public class Product
    {
        [Key]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(Store))]
        public Guid StoreId { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string? Image { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        // ---------- Navigation Properties ----------

        public Store? Store { get; set; }

        public ICollection<Review>? Reviews { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
