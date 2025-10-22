using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.Models.Store;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Catalog
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new product: {ProductName}", product?.Name);
            
            try
            {
                // Validate input
                if (product == null)
                {
                    _logger.LogWarning("Attempted to add null product");
                    throw new ArgumentNullException(nameof(product));
                }

                _logger.LogDebug("Adding product to database context. ProductId: {ProductId}, Name: {ProductName}, Category: {Category}, Price: {Price}", 
                    product.ProductId, product.Name, product.Category ?? "Uncategorized", product.Price);

                // Add product to context
                _context.Products.Add(product);

                _logger.LogInformation("Successfully added product. ProductId: {ProductId}, Name: {ProductName}",
                    product.ProductId, product.Name);

                return Task.FromResult(product);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding product: {ProductName}", product?.Name);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating product: {ProductName}", product?.Name);
                throw new InvalidOperationException($"Unexpected error while creating product: {ex.Message}", ex);
            }
        }

        public async Task<Product> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update product: {ProductId}", product?.ProductId);
            
            try
            {
                // Validate input
                if (product == null)
                {
                    _logger.LogWarning("Attempted to update null product");
                    throw new ArgumentNullException(nameof(product));
                }

                if (product.ProductId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update product with empty ID");
                    throw new ArgumentException("Product ID cannot be empty", nameof(product));
                }

                _logger.LogDebug("Looking for existing product: {ProductId}", product.ProductId);

                // Check if product exists (use FindAsync for simple PK lookup)
                var existingProduct = await _context.Products.FindAsync(new object[] { product.ProductId }, cancellationToken);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product not found for update. ProductId: {ProductId}", product.ProductId);
                    throw new InvalidOperationException($"Product with ID {product.ProductId} not found");
                }

                _logger.LogInformation("Found product for update. Current: {CurrentName} -> New: {NewName}", 
                    existingProduct.Name, product.Name);

                // Update only specific properties (not foreign keys or primary key) - only if provided
                if (!string.IsNullOrEmpty(product.Name))
                {
                    existingProduct.Name = product.Name;
                }

                if (product.Description != null)
                {
                    existingProduct.Description = product.Description;
                }

                if (product.Price > 0) // Only update if price is provided and valid
                {
                    existingProduct.Price = product.Price;
                }

                if (product.StockQuantity >= 0) // Only update if stock quantity is provided and valid
                {
                    existingProduct.StockQuantity = product.StockQuantity;
                }

                if (product.Image != null)
                {
                    existingProduct.Image = product.Image;
                }

                if (!string.IsNullOrEmpty(product.Category))
                {
                    existingProduct.Category = product.Category;
                }
                

                _logger.LogInformation("Successfully updated product. ProductId: {ProductId}, Name: {ProductName}", 
                    existingProduct.ProductId, existingProduct.Name);

                return existingProduct;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating product: {ProductId}", product?.ProductId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Product not found while updating: {ProductId}", product?.ProductId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating product: {ProductId}", product?.ProductId);
                throw new InvalidOperationException($"Unexpected error while updating product with ID {product?.ProductId}: {ex.Message}", ex);
            }
        }

        public async Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving product by ID: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve product with empty ID");
                    return null; // Invalid ID
                }

                // Find the product with navigation properties
                var product = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

                if (product != null)
                {
                    _logger.LogDebug("Product found: {ProductName} (ID: {ProductId})", product.Name, productId);
                }
                else
                {
                    _logger.LogDebug("Product not found with ID: {ProductId}", productId);
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product with ID: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to retrieve product with ID {productId}: {ex.Message}", ex);
            }
        }


        public async Task<List<Product>> GetProductsByStoreIdAsync(Guid storeId, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products for store: {StoreId}, IncludeDeleted: {IncludeDeleted}", storeId, includeDeleted);

            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve products with empty store ID");
                    return new List<Product>(); // Invalid ID, return empty list
                }

                var query = _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.StoreId == storeId);

                // Include soft-deleted products if requested
                if (includeDeleted)
                {
                    query = query.IgnoreQueryFilters();
                }

                var products = await query.ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products for store {StoreId}, IncludeDeleted: {IncludeDeleted}",
                    products.Count, storeId, includeDeleted);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to retrieve products for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving all products");
            
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {ProductCount} products", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all products");
                throw new InvalidOperationException($"Failed to retrieve all products: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products by price range: {MinPrice:C} to {MaxPrice:C}", minPrice, maxPrice);
            
            try
            {
                // Validate price range
                if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
                {
                    _logger.LogWarning("Invalid price range provided: {MinPrice:C} to {MaxPrice:C}", minPrice, maxPrice);
                    throw new ArgumentException("Invalid price range");
                }

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products in price range {MinPrice:C} to {MaxPrice:C}", 
                    products.Count, minPrice, maxPrice);

                return products;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in price range: {MinPrice:C} to {MaxPrice:C}", minPrice, maxPrice);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products for price range {MinPrice:C} to {MaxPrice:C}", minPrice, maxPrice);
                throw new InvalidOperationException($"Failed to retrieve products for price range {minPrice:C} to {maxPrice:C}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsInStockAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products in stock");
            
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Where(p => p.StockQuantity > 0)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products in stock", products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products in stock");
                throw new InvalidOperationException($"Failed to retrieve products in stock: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsOutOfStockAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products out of stock");
            
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Where(p => p.StockQuantity == 0)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products out of stock", products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products out of stock");
                throw new InvalidOperationException($"Failed to retrieve products out of stock: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetFilteredProductsAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving filtered products with custom predicate");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to filter products with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products using custom filter", products.Count);

                return products;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in filtered products query");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered products");
                throw new InvalidOperationException($"Failed to retrieve filtered products: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateStockQuantityAsync(Guid productId, int newQuantity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating stock quantity for product: {ProductId} to {NewQuantity}", productId, newQuantity);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update stock with empty product ID");
                    return false; // Invalid ID
                }

                if (newQuantity < 0)
                {
                    _logger.LogWarning("Attempted to set negative stock quantity: {NewQuantity} for product: {ProductId}", newQuantity, productId);
                    throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for stock update: {ProductId}", productId);
                    return false; // Product not found
                }

                var oldQuantity = product.StockQuantity;
                product.StockQuantity = newQuantity;

                _logger.LogInformation("Successfully updated stock quantity for product: {ProductId}, {OldQuantity} -> {NewQuantity}", 
                    productId, oldQuantity, newQuantity);

                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating stock quantity for product: {ProductId}", productId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stock quantity for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to update stock quantity for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int threshold, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products with low stock (threshold: {Threshold})", threshold);

            try
            {
                var products = await _context.Products
                    .Where(p => p.StockQuantity <= threshold)
                    .OrderBy(p => p.StockQuantity)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} products with low stock", products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve low stock products with threshold: {Threshold}", threshold);
                throw new InvalidOperationException($"Failed to retrieve low stock products: {ex.Message}", ex);
            }
        }

        public async Task<bool> SoftDeleteProductAsync(Guid productId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Soft deleting product: {ProductId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                productId, deletedBy, reason);

            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete product with empty ID");
                    return false;
                }

                if (deletedBy == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to soft delete product with empty DeletedBy ID");
                    return false;
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for soft deletion: {ProductId}", productId);
                    return false;
                }

                // Note: With HasQueryFilter, we won't find already soft-deleted products
                // So we don't need to check if (product.IsDeleted) anymore

                // Perform soft delete
                product.IsDeleted = true;
                product.DeletedAt = DateTime.UtcNow;
                product.DeletedBy = deletedBy;
                product.DeletedReason = reason;


                _logger.LogInformation("Successfully soft deleted product: {ProductId}, Name: {ProductName}", 
                    productId, product.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete product: {ProductId}, DeletedBy: {DeletedBy}", productId, deletedBy);
                throw new InvalidOperationException($"Failed to soft delete product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> RestoreProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Restoring product: {ProductId}", productId);

            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to restore product with empty ID");
                    return false;
                }

                var product = await _context.Products
                    .IgnoreQueryFilters() // Include soft-deleted products
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for restoration: {ProductId}", productId);
                    return false;
                }

                if (!product.IsDeleted)
                {
                    _logger.LogWarning("Product is not deleted, cannot restore: {ProductId}", productId);
                    return true; // Already restored, consider it successful
                }

                // Restore product
                product.IsDeleted = false;
                product.DeletedAt = null;
                product.DeletedBy = null;
                product.DeletedReason = null;


                _logger.LogInformation("Successfully restored product: {ProductId}, Name: {ProductName}", 
                    productId, product.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to restore product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<Product?> GetProductByIdIncludeDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving product by ID (including deleted): {ProductId}", productId);

            try
            {
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve product with empty ID");
                    return null;
                }

                var product = await _context.Products
                    .IgnoreQueryFilters() // Include soft-deleted products
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogDebug("Product not found (including deleted): {ProductId}", productId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved product (including deleted): {ProductId}, Name: {ProductName}, IsDeleted: {IsDeleted}", 
                        productId, product.Name, product.IsDeleted);
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product (including deleted): {ProductId}", productId);
                throw new InvalidOperationException($"Failed to retrieve product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> HardDeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogCritical("HARD DELETING product: {ProductId} - This action is IRREVERSIBLE", productId);

            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to hard delete product with empty ID");
                    return false;
                }

                // Get product including soft-deleted ones for hard deletion
                var product = await _context.Products
                    .IgnoreQueryFilters() // Include soft-deleted products
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for hard deletion: {ProductId}", productId);
                    return false;
                }

                // Log product details before permanent deletion for audit
                _logger.LogCritical("Hard deleting product details - Name: {ProductName}, StoreId: {StoreId}, CreatedAt: {CreatedAt}, WasDeleted: {IsDeleted}", 
                    product.Name, product.StoreId, product.CreatedAt, product.IsDeleted);

                // Perform hard delete (permanent removal from database)
                _context.Products.Remove(product);

                _logger.LogCritical("HARD DELETE COMPLETED. Product permanently removed from database. ProductId: {ProductId}, Name: {ProductName}", 
                    productId, product.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hard delete product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to hard delete product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ProductPerformance>> GetTopPerformingProductsAsync(Guid storeId, int count = 10, CancellationToken cancellationToken = default)
        {
            return await GetTopPerformingProductsAsync(storeId, count, null, cancellationToken);
        }

        public async Task<List<ProductPerformance>> GetTopPerformingProductsAsync(Guid storeId, int count, DateTime? performancePeriodStart, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting top performing products for store: {StoreId}, count: {Count}, performancePeriodStart: {PerformancePeriodStart}", 
                storeId, count, performancePeriodStart);
            
            try
            {
                // Default to last 12 months if no period specified
                var periodStart = performancePeriodStart ?? DateTime.UtcNow.AddMonths(-12);
                
                _logger.LogDebug("Using performance period: {PeriodStart} to {PeriodEnd}", periodStart, DateTime.UtcNow);

                // Get top performing products by revenue using efficient SQL aggregation with time-based filtering
                var query = _context.Products
                    .Where(p => p.StoreId == storeId && !p.IsDeleted)
                    .Select(p => new ProductPerformance
                    {
                        ProductId = p.ProductId,
                        ProductName = p.Name,
                        // Only include OrderItems from the specified time period
                        UnitsSold = p.OrderItems != null 
                            ? p.OrderItems.Where(oi => oi.Order != null && oi.Order.Date >= periodStart).Sum(oi => oi.Quantity) 
                            : 0,
                        Revenue = p.OrderItems != null 
                            ? p.OrderItems.Where(oi => oi.Order != null && oi.Order.Date >= periodStart).Sum(oi => oi.Price * oi.Quantity) 
                            : 0,
                        // Only include Reviews from the specified time period
                        AverageRating = p.Reviews != null && p.Reviews.Any(r => r.CreatedAt >= periodStart) 
                            ? p.Reviews.Where(r => r.CreatedAt >= periodStart).Average(r => (decimal)r.Rating) 
                            : 0,
                        ReviewCount = p.Reviews != null 
                            ? p.Reviews.Count(r => r.CreatedAt >= periodStart) 
                            : 0
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(count)
                    .AsNoTracking(); // Read-only query for better performance

                #if DEBUG
                var sqlQuery = query.ToQueryString();
                _logger.LogDebug("Generated SQL Query for top performing products: {SQL}", sqlQuery);
                #endif

                var topProducts = await query.ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {Count} top performing products for store: {StoreId}", 
                    topProducts.Count, storeId);

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get top performing products for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to get top performing products for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetProductCountByStoreIdAsync(Guid storeId, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting product count for store: {StoreId}, includeDeleted: {IncludeDeleted}", storeId, includeDeleted);
            
            try
            {
                var query = _context.Products.Where(p => p.StoreId == storeId);
                
                if (!includeDeleted)
                {
                    // Use the global query filter for soft deletion
                    // No additional filtering needed as HasQueryFilter handles this
                }
                else
                {
                    // Include deleted products by ignoring the global query filter
                    query = query.IgnoreQueryFilters();
                }

                var count = await query.CountAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved product count for store: {StoreId}. Count: {Count}, IncludeDeleted: {IncludeDeleted}", 
                    storeId, count, includeDeleted);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product count for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to get product count for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<ProductSalesStats> GetProductSalesStatsAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting product sales stats for product: {ProductId}, from {StartDate} to {EndDate}", productId, startDate, endDate);
            
            try
            {
                // Get aggregated sales data using efficient SQL aggregation
                var salesData = await _context.OrderItems
                    .Where(oi => oi.ProductId == productId)
                    .Where(oi => oi.Order != null && oi.Order.Date >= startDate && oi.Order.Date <= endDate)
                    .Select(oi => new
                    {
                        oi.Quantity,
                        oi.Price,
                        Revenue = oi.Price * oi.Quantity,
                        OrderDate = oi.Order!.Date
                    })
                    .AsNoTracking() // Read-only query for better performance
                    .ToListAsync(cancellationToken);

                var totalSales = salesData.Sum(s => s.Quantity);
                var totalRevenue = salesData.Sum(s => s.Revenue);

                // Calculate monthly data
                var monthlyData = salesData
                    .GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
                    .Select(g => new MonthlyProductSalesData
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Sales = g.Sum(s => s.Quantity),
                        Revenue = g.Sum(s => s.Revenue),
                        UnitsSold = g.Sum(s => s.Quantity)
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();

                var result = new ProductSalesStats
                {
                    TotalSales = totalSales,
                    TotalRevenue = totalRevenue,
                    MonthlyData = monthlyData
                };

                _logger.LogDebug("Successfully retrieved product sales stats for product: {ProductId}. TotalSales: {TotalSales}, TotalRevenue: {TotalRevenue}", 
                    productId, totalSales, totalRevenue);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product sales stats for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to get product sales stats for product {productId}: {ex.Message}", ex);
            }
        }

        public IQueryable<Product> GetProductsQueryable()
        {
            return _context.Products
                .Include(p => p.Store)
                .Include(p => p.Reviews);
        }

        public IQueryable<Product> GetProductsQueryableIgnoreFilters()
        {
            return _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Store)
                .Include(p => p.Reviews);
        }

        public async Task<int> GetProductsCountAsync(IQueryable<Product> query, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting product count from queryable");
            
            try
            {
                var count = await query.CountAsync(cancellationToken);
                _logger.LogDebug("Successfully got product count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product count from queryable");
                throw new InvalidOperationException($"Failed to get product count: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsPagedAsync(IQueryable<Product> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting paged products. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            
            try
            {
                var products = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully got {Count} paged products", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get paged products. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
                throw new InvalidOperationException($"Failed to get paged products: {ex.Message}", ex);
            }
        }
    }
}
