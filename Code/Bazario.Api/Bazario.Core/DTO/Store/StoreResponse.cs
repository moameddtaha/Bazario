using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Core.DTO
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

        public override bool Equals(object? obj)
        {
            return obj is StoreResponse response &&
                   StoreId.Equals(response.StoreId) &&
                   SellerId.Equals(response.SellerId) &&
                   Name == response.Name &&
                   Description == response.Description &&
                   Category == response.Category &&
                   Logo == response.Logo &&
                   CreatedAt == response.CreatedAt;
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
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Store: ID: {StoreId}, Name: {Name}, Category: {Category}, Seller ID: {SellerId}";
        }

        public StoreUpdateRequest ToStoreUpdateRequest()
        {
            return new StoreUpdateRequest()
            {
                StoreId = StoreId,
                Name = Name,
                Description = Description,
                Category = Category,
                Logo = Logo
            };
        }
    }
}
