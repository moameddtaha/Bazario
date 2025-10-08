using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.Entities.Review
{
    public class Review
    {
        [Key]
        public Guid ReviewId { get; set; }

        [ForeignKey(nameof(Customer))]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(Product))]
        public Guid ProductId { get; set; }

        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        // ---------- Navigation Properties ----------

        public ApplicationUser? Customer { get; set; }

        public Product? Product { get; set; }
    }
}
