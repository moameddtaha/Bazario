using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.DTO.Catalog.Product
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

        // ---------- Concurrency Control ----------
        [Display(Name = "Row Version")]
        public byte[]? RowVersion { get; set; }

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

        [Display(Name = "In Stock")]
        public bool IsInStock => StockQuantity > 0;

        public override bool Equals(object? obj)
        {
            if (obj is not ProductResponse response)
                return false;

            // Compare RowVersion byte arrays
            bool rowVersionEquals = (RowVersion == null && response.RowVersion == null) ||
                                   (RowVersion != null && response.RowVersion != null &&
                                    RowVersion.SequenceEqual(response.RowVersion));

            return ProductId.Equals(response.ProductId) &&
                   StoreId.Equals(response.StoreId) &&
                   Name == response.Name &&
                   Description == response.Description &&
                   Price == response.Price &&
                   StockQuantity == response.StockQuantity &&
                   Image == response.Image &&
                   Category == response.Category &&
                   CreatedAt == response.CreatedAt &&
                   rowVersionEquals &&
                   IsDeleted == response.IsDeleted &&
                   DeletedAt == response.DeletedAt &&
                   DeletedBy == response.DeletedBy &&
                   DeletedReason == response.DeletedReason;
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
            // RowVersion not included in hash - concurrency tokens typically excluded
            hash.Add(IsDeleted);
            hash.Add(DeletedAt);
            hash.Add(DeletedBy);
            hash.Add(DeletedReason);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Product: ID: {ProductId}, Name: {Name}, Price: {Price:C}, Stock: {StockQuantity}, Store ID: {StoreId}, Category: {Category}, IsDeleted: {IsDeleted}";
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
                Category = Category,
                RowVersion = RowVersion
            };
        }
    }
}
