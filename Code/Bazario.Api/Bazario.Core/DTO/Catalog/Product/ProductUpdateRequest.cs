using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums.Catalog;
using ProductEntity = Bazario.Core.Domain.Entities.Catalog.Product;

namespace Bazario.Core.DTO.Catalog.Product
{
    public class ProductUpdateRequest
    {
        [Required(ErrorMessage = "Product Id cannot be blank")]
        public Guid ProductId { get; set; }

        [StringLength(30, ErrorMessage = "Name cannot exceed 30 characters")]
        [Display(Name = "Product Name")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity cannot be negative")]
        public int? StockQuantity { get; set; }

        [Display(Name = "Image")]
        public string? Image { get; set; }

        [Display(Name = "Category")]
        public Category? Category { get; set; }

        public ProductEntity ToProduct()
        {
            return new ProductEntity
            {
                ProductId = ProductId,
                Name = Name,
                Description = Description,
                Price = Price ?? -1, // Use -1 to indicate not provided (repository will check for > 0)
                StockQuantity = StockQuantity ?? -1, // Use -1 to indicate not provided (repository will check for >= 0)
                Image = Image,
                Category = Category?.ToString()
            };
        }
    }
}
