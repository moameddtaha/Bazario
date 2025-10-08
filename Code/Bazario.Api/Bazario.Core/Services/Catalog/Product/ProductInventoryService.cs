using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.DTO.Catalog.Product;
using Bazario.Core.Extensions.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product inventory management
    /// Handles stock updates, reservations, and releases
    /// </summary>
    public class ProductInventoryService : IProductInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductInventoryService> _logger;

        public ProductInventoryService(
            IProductRepository productRepository,
            ILogger<ProductInventoryService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductResponse> UpdateStockAsync(Guid productId, int newQuantity, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating stock for product: {ProductId}, NewQuantity: {NewQuantity}, Reason: {Reason}", 
                productId, newQuantity, reason);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (newQuantity < 0)
                {
                    throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Reason is required for stock updates", nameof(reason));
                }

                // Get existing product
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Stock update failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                var oldQuantity = product.StockQuantity;
                product.StockQuantity = newQuantity;

                _logger.LogDebug("Updating stock from {OldQuantity} to {NewQuantity} for product: {ProductId}", 
                    oldQuantity, newQuantity, productId);

                // Save to repository
                var updatedProduct = await _productRepository.UpdateProductAsync(product, cancellationToken);

                _logger.LogInformation("Successfully updated stock for product: {ProductId}, OldQuantity: {OldQuantity}, NewQuantity: {NewQuantity}, Reason: {Reason}", 
                    productId, oldQuantity, newQuantity, reason);

                return updatedProduct.ToProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stock for product: {ProductId}, NewQuantity: {NewQuantity}", productId, newQuantity);
                throw;
            }
        }

        public async Task<bool> ReserveStockAsync(Guid productId, int quantity, Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reserving stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}", 
                productId, quantity, orderId);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (quantity <= 0)
                {
                    throw new ArgumentException("Reservation quantity must be greater than 0", nameof(quantity));
                }

                if (orderId == Guid.Empty)
                {
                    throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
                }

                // Get existing product
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Stock reservation failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                // Check if sufficient stock is available
                if (product.StockQuantity < quantity)
                {
                    _logger.LogWarning("Stock reservation failed: Insufficient stock. ProductId: {ProductId}, Available: {Available}, Requested: {Requested}", 
                        productId, product.StockQuantity, quantity);
                    throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}, Requested: {quantity}");
                }

                // Reserve stock (reduce available quantity)
                var newQuantity = product.StockQuantity - quantity;
                product.StockQuantity = newQuantity;

                _logger.LogDebug("Reserving {Quantity} units of product: {ProductId}, Remaining stock: {RemainingStock}", 
                    quantity, productId, newQuantity);

                // Save to repository
                await _productRepository.UpdateProductAsync(product, cancellationToken);

                _logger.LogInformation("Successfully reserved stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}, Remaining: {RemainingStock}", 
                    productId, quantity, orderId, newQuantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reserve stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}", 
                    productId, quantity, orderId);
                throw;
            }
        }

        public async Task<bool> ReleaseStockAsync(Guid productId, int quantity, Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Releasing stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}", 
                productId, quantity, orderId);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (quantity <= 0)
                {
                    throw new ArgumentException("Release quantity must be greater than 0", nameof(quantity));
                }

                if (orderId == Guid.Empty)
                {
                    throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
                }

                // Get existing product
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Stock release failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                // Release stock (increase available quantity)
                var newQuantity = product.StockQuantity + quantity;
                product.StockQuantity = newQuantity;

                _logger.LogDebug("Releasing {Quantity} units of product: {ProductId}, New stock: {NewStock}", 
                    quantity, productId, newQuantity);

                // Save to repository
                await _productRepository.UpdateProductAsync(product, cancellationToken);

                _logger.LogInformation("Successfully released stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}, New stock: {NewStock}", 
                    productId, quantity, orderId, newQuantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release stock for product: {ProductId}, Quantity: {Quantity}, OrderId: {OrderId}", 
                    productId, quantity, orderId);
                throw;
            }
        }
    }
}
