using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.Entities
{
    public class Store
    {
        [Key]
        public Guid StoreId { get; set; }

        [ForeignKey(nameof(Seller))]
        public Guid SellerId { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? LogoUrl { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        // ---------- Navigation Properties ----------

        public ApplicationUser? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
