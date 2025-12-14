using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;

namespace Bazario.Core.DTO.Store
{
    public class StoreUpdateRequest
    {
        [Required(ErrorMessage = "Store Id cannot be blank")]
        public Guid StoreId { get; set; }

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

        [Display(Name = "Is Active")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// </summary>
        public byte[]? RowVersion { get; set; }

        public StoreEntity ToStore()
        {
            return new StoreEntity
            {
                StoreId = StoreId,
                Name = Name ?? string.Empty, // Provide default if null
                Description = Description ?? string.Empty, // Provide default if null
                Category = Category ?? string.Empty, // Provide default if null
                Logo = Logo ?? string.Empty, // Provide default if null
                IsActive = IsActive ?? true, // Default to true if not specified
                RowVersion = RowVersion
            };
        }
    }
}
