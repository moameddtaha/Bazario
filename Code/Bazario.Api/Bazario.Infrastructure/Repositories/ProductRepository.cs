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
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
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

        public async Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default)
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
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added product. ProductId: {ProductId}, Name: {ProductName}", 
                    product.ProductId, product.Name);

                return product;
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
                
                await _context.SaveChangesAsync(cancellationToken);

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

        public async Task<bool> DeleteProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete product with empty ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Looking for product to delete: {ProductId}", productId);

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for deletion. ProductId: {ProductId}", productId);
                    return false; // Product not found
                }

                _logger.LogInformation("Found product for deletion. Name: {ProductName}", product.Name);

                // Delete the product
                _context.Products.Remove(product);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted product. ProductId: {ProductId}, Name: {ProductName}", 
                    productId, product.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting product: {ProductId}", productId);
                throw new InvalidOperationException($"Unexpected error while deleting product with ID {productId}: {ex.Message}", ex);
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
                _logger.LogError(ex, "Failed to retrieve all products");
                throw new InvalidOperationException($"Failed to retrieve products: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving products for store: {StoreId}", storeId);
            
            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve products with empty store ID");
                    return new List<Product>(); // Invalid ID, return empty list
                }

                var products = await _context.Products
                    .Include(p => p.Store)
                    .Include(p => p.Reviews)
                    .Include(p => p.OrderItems)
                    .Where(p => p.StoreId == storeId)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {ProductCount} products for store {StoreId}", products.Count, storeId);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve products for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to retrieve products for store {storeId}: {ex.Message}", ex);
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
                    .Include(p => p.OrderItems)
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
                    .Include(p => p.OrderItems)
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
                    .Include(p => p.OrderItems)
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
                    .Include(p => p.OrderItems)
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

                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for stock update: {ProductId}", productId);
                    return false; // Product not found
                }

                var oldQuantity = product.StockQuantity;
                product.StockQuantity = newQuantity;
                await _context.SaveChangesAsync(cancellationToken);

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
    }
}
