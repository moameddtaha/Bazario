using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Order;
using Bazario.Core.Domain.IdentityEntities;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;
using ReviewEntity = Bazario.Core.Domain.Entities.Review.Review;


namespace Bazario.Core.Domain.Entities.Catalog
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

        public string? Category { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        // ---------- Soft Deletion Properties ----------
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        [StringLength(500)]
        public string? DeletedReason { get; set; }

        // ---------- Navigation Properties ----------

        public StoreEntity? Store { get; set; }

        public ICollection<ReviewEntity>? Reviews { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
