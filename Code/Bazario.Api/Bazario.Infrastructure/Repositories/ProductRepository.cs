using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (product == null)
                    throw new ArgumentNullException(nameof(product));

                // Add product to context
                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                return product;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating product: {ex.Message}", ex);
            }
        }

        public async Task<Product> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (product == null)
                    throw new ArgumentNullException(nameof(product));

                if (product.ProductId == Guid.Empty)
                    throw new ArgumentException("Product ID cannot be empty", nameof(product));

                // Check if product exists (use FindAsync for simple PK lookup)
                var existingProduct = await _context.Products.FindAsync(new object[] { product.ProductId }, cancellationToken);
                if (existingProduct == null)
                {
                    throw new InvalidOperationException($"Product with ID {product.ProductId} not found");
                }

                // Update only specific properties (not foreign keys or primary key)
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Image = product.Image;
                
                await _context.SaveChangesAsync(cancellationToken);

                return existingProduct;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while updating product with ID {product?.ProductId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product == null)
                {
                    return false; // Product not found
                }

                // Delete the product
                _context.Products.Remove(product);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting product with ID {productId}: {ex.Message}", ex);
            }
        }

        public async Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return null; // Invalid ID
                }

                // Find the product with navigation properties
                var product = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

                return product;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve product with ID {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve products: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    return new List<Product>(); // Invalid ID, return empty list
                }

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.StoreId == storeId)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve products for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate price range
                if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
                {
                    throw new ArgumentException("Invalid price range");
                }

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve products for price range {minPrice:C} to {maxPrice:C}: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsInStockAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.StockQuantity > 0)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve products in stock: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsOutOfStockAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.StockQuantity == 0)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve products out of stock: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetFilteredProductsAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered products: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateStockQuantityAsync(Guid productId, int newQuantity, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                if (newQuantity < 0)
                {
                    throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));
                }

                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product == null)
                {
                    return false; // Product not found
                }

                product.StockQuantity = newQuantity;
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update stock quantity for product {productId}: {ex.Message}", ex);
            }
        }
    }
}
