using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Domain.RepositoryContracts.Catalog
{
    public interface IProductRepository
    {
        Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default);

        Task<Product> UpdateProductAsync(Product product, CancellationToken cancellationToken = default);

        Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a product by ID with its Store navigation property eagerly loaded (single query optimization)
        /// </summary>
        Task<Product?> GetProductWithStoreByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsByStoreIdAsync(Guid storeId, bool includeDeleted = false, CancellationToken cancellationToken = default);

        Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsInStockAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetProductsOutOfStockAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetFilteredProductsAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> UpdateStockQuantityAsync(Guid productId, int newQuantity, CancellationToken cancellationToken = default);

        Task<List<Product>> GetLowStockProductsAsync(int threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top performing products for a specific store with aggregated data
        /// </summary>
        Task<List<ProductPerformance>> GetTopPerformingProductsAsync(Guid storeId, int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top performing products for a specific store with aggregated data and configurable time period
        /// </summary>
        Task<List<ProductPerformance>> GetTopPerformingProductsAsync(Guid storeId, int count, DateTime? performancePeriodStart, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of products for a specific store with optional soft deletion filtering
        /// </summary>
        Task<int> GetProductCountByStoreIdAsync(Guid storeId, bool includeDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated sales statistics for a specific product
        /// </summary>
        Task<ProductSalesStats> GetProductSalesStatsAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable collection of products for efficient query composition
        /// </summary>
        IQueryable<Product> GetProductsQueryable();

        /// <summary>
        /// Gets a queryable collection of products ignoring soft deletion filters
        /// </summary>
        IQueryable<Product> GetProductsQueryableIgnoreFilters();

        /// <summary>
        /// Gets a read-only queryable collection of products with change tracking disabled (optimized for queries)
        /// </summary>
        IQueryable<Product> GetProductsQueryableAsNoTracking();

        /// <summary>
        /// Gets a read-only queryable collection of products ignoring soft deletion filters with change tracking disabled (optimized for queries)
        /// </summary>
        IQueryable<Product> GetProductsQueryableIgnoreFiltersAsNoTracking();

        /// <summary>
        /// Gets the count of products from a queryable collection
        /// </summary>
        Task<int> GetProductsCountAsync(IQueryable<Product> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paged products from a queryable collection
        /// </summary>
        Task<List<Product>> GetProductsPagedAsync(IQueryable<Product> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Soft Deletion Methods
        Task<bool> SoftDeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);

        Task<bool> RestoreProductAsync(Guid productId, Guid restoredBy, CancellationToken cancellationToken = default);

        Task<Product?> GetProductByIdIncludeDeletedAsync(Guid productId, CancellationToken cancellationToken = default);

        // Hard Deletion Methods
        Task<bool> HardDeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
