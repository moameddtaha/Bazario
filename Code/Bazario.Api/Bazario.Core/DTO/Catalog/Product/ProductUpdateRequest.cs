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

        /// <summary>
        /// Row version for optimistic concurrency control
        /// Must be provided for updates to prevent lost updates
        /// </summary>
        [Display(Name = "Row Version")]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Converts this DTO to a Product entity for partial updates
        /// NOTE: This uses sentinel values (-1) to indicate "not provided" fields
        /// - Price: -1 means "don't update" (repository checks > 0)
        /// - StockQuantity: -1 means "don't update" (repository checks >= 0)
        /// This pattern is maintained for consistency with existing repository implementation
        /// Consider refactoring to use a proper partial update pattern in future versions
        /// </summary>
        public ProductEntity ToProduct()
        {
            return new ProductEntity
            {
                ProductId = ProductId,
                Name = Name,
                Description = Description,
                Price = Price ?? -1, // Sentinel: -1 indicates not provided (repository checks > 0)
                StockQuantity = StockQuantity ?? -1, // Sentinel: -1 indicates not provided (repository checks >= 0)
                Image = Image,
                Category = Category?.ToString(),
                RowVersion = RowVersion // Required for optimistic concurrency control
            };
        }
    }
}
