using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.DTO
{
    public class StoreUpdateRequest
    {
        [Required(ErrorMessage = "Store Id cannot be blank")]
        public Guid StoreId { get; set; }

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

        public Store ToStore()
        {
            return new Store
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
