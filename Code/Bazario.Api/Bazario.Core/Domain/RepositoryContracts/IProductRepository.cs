using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface IProductRepository
    {
        Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default);

        Task<Product> UpdateProductAsync(Product product, CancellationToken cancellationToken = default);

        Task<bool> DeleteProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsInStockAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsOutOfStockAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetFilteredProductsAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> UpdateStockQuantityAsync(Guid productId, int newQuantity, CancellationToken cancellationToken = default);
    }
}
