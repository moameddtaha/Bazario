using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums;

namespace Bazario.Core.DTO
{
    public class ProductResponse
    {
        public Guid ProductId { get; set; }

        [Display(Name = "Store Id")]
        public Guid StoreId { get; set; }

        [Display(Name = "Product Name")]
        public string? Name { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Image")]
        public string? Image { get; set; }

        [Display(Name = "Category")]
        public Category Category { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "In Stock")]
        public bool IsInStock => StockQuantity > 0;

        public override bool Equals(object? obj)
        {
            return obj is ProductResponse response &&
                   ProductId.Equals(response.ProductId) &&
                   StoreId.Equals(response.StoreId) &&
                   Name == response.Name &&
                   Description == response.Description &&
                   Price == response.Price &&
                   StockQuantity == response.StockQuantity &&
                   Image == response.Image &&
                   Category == response.Category &&
                   CreatedAt == response.CreatedAt;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(ProductId);
            hash.Add(StoreId);
            hash.Add(Name);
            hash.Add(Description);
            hash.Add(Price);
            hash.Add(StockQuantity);
            hash.Add(Image);
            hash.Add(Category);
            hash.Add(CreatedAt);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Product: ID: {ProductId}, Name: {Name}, Price: {Price:C}, Stock: {StockQuantity}, Store ID: {StoreId}, Category: {Category}";
        }

        public ProductUpdateRequest ToProductUpdateRequest()
        {
            return new ProductUpdateRequest()
            {
                ProductId = ProductId,
                Name = Name,
                Description = Description,
                Price = Price,
                StockQuantity = StockQuantity,
                Image = Image,
                Category = Category
            };
        }
    }
}
