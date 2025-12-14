using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Core.DTO.Store
{
    public class StoreResponse
    {
        public Guid StoreId { get; set; }

        [Display(Name = "Seller Id")]
        public Guid SellerId { get; set; }

        [Display(Name = "Store Name")]
        public string? Name { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Logo")]
        public string? Logo { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        // ---------- Soft Deletion Properties ----------

        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        [Display(Name = "Deleted At")]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        [Display(Name = "Deleted By")]
        public Guid? DeletedBy { get; set; }

        [Display(Name = "Deleted Reason")]
        public string? DeletedReason { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// </summary>
        public byte[]? RowVersion { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is StoreResponse response &&
                   StoreId.Equals(response.StoreId) &&
                   SellerId.Equals(response.SellerId) &&
                   Name == response.Name &&
                   Description == response.Description &&
                   Category == response.Category &&
                   Logo == response.Logo &&
                   CreatedAt == response.CreatedAt &&
                   IsActive == response.IsActive &&
                   IsDeleted == response.IsDeleted &&
                   DeletedAt == response.DeletedAt &&
                   DeletedBy == response.DeletedBy &&
                   DeletedReason == response.DeletedReason;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(StoreId);
            hash.Add(SellerId);
            hash.Add(Name);
            hash.Add(Description);
            hash.Add(Category);
            hash.Add(Logo);
            hash.Add(CreatedAt);
            hash.Add(IsActive);
            hash.Add(IsDeleted);
            hash.Add(DeletedAt);
            hash.Add(DeletedBy);
            hash.Add(DeletedReason);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            var status = IsDeleted ? " (DELETED)" : IsActive ? " (ACTIVE)" : " (INACTIVE)";
            return $"Store: ID: {StoreId}, Name: {Name}, Category: {Category}, Seller ID: {SellerId}{status}";
        }

        public StoreUpdateRequest ToStoreUpdateRequest()
        {
            return new StoreUpdateRequest()
            {
                StoreId = StoreId,
                Name = Name,
                Description = Description,
                Category = Category,
                Logo = Logo,
                RowVersion = RowVersion
            };
        }
    }
}
