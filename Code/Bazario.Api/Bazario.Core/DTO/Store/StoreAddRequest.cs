using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using StoreEntity = Bazario.Core.Domain.Entities.Store;

namespace Bazario.Core.DTO.Store
{
    public class StoreAddRequest
    {
        [Required(ErrorMessage = "Seller Id cannot be blank")]
        [Display(Name = "Seller Id")]
        public Guid SellerId { get; set; }

        [Required(ErrorMessage = "Name cannot be blank")]
        [StringLength(30, ErrorMessage = "Name cannot exceed 30 characters")]
        [Display(Name = "Store Name")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Logo")]
        public string? Logo { get; set; }

        public StoreEntity ToStore()
        {
            return new StoreEntity
            {
                SellerId = SellerId,
                Name = Name,
                Description = Description,
                Category = Category,
                Logo = Logo,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
