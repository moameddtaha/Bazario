using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.Extensions.Catalog
{
    public static class ProductExtensions
    {
        /// <summary>
        /// Converts a Product entity to a ProductResponse DTO
        /// Defensive: Returns null if product is null (prevents NullReferenceException)
        /// </summary>
        public static ProductResponse? ToProductResponse(this Product? product)
        {
            if (product == null)
            {
                return null;
            }

            return new ProductResponse
            {
                ProductId = product.ProductId,
                StoreId = product.StoreId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Image = product.Image,
                Category = Enum.TryParse<Category>(product.Category, out var category) ? category : Category.uncategorized,
                CreatedAt = product.CreatedAt,
                // Concurrency Control
                RowVersion = product.RowVersion,
                // Soft Deletion Properties
                IsDeleted = product.IsDeleted,
                DeletedAt = product.DeletedAt,
                DeletedBy = product.DeletedBy,
                DeletedReason = product.DeletedReason
            };
        }
    }
}
