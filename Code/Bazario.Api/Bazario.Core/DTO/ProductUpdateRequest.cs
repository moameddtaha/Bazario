using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.DTO
{
    public class ProductUpdateRequest
    {
        [Required(ErrorMessage = "Product Id cannot be blank")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Name cannot be blank")]
        [StringLength(30, ErrorMessage = "Name cannot exceed 30 characters")]
        [Display(Name = "Product Name")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price cannot be blank")]
        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock Quantity cannot be blank")]
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity cannot be negative")]
        public int StockQuantity { get; set; }

        [Display(Name = "Image")]
        public string? Image { get; set; }

        public Product ToProduct()
        {
            return new Product
            {
                ProductId = ProductId,
                Name = Name,
                Description = Description,
                Price = Price,
                StockQuantity = StockQuantity,
                Image = Image
            };
        }
    }
}
