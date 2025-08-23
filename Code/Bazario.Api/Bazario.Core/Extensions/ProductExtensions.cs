using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.DTO;
using Bazario.Core.Enums;

namespace Bazario.Core.Extensions
{
    public static class ProductExtensions
    {
        public static ProductResponse ToProductResponse(this Product product)
        {
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
                CreatedAt = product.CreatedAt
            };
        }
    }
}
