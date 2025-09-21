using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Product;
using Bazario.Core.ServiceContracts.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Product
{
    /// <summary>
    /// Service implementation for product validation operations
    /// Handles product validation logic and business rules
    /// </summary>
    public class ProductValidationService : IProductValidationService
    {
        private readonly IProductRepository _productRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly ILogger<ProductValidationService> _logger;

        public ProductValidationService(
            IProductRepository productRepository,
            IStoreRepository storeRepository,
            ILogger<ProductValidationService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating product for order: ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);

            try
            {
                // Validate inputs
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                if (quantity <= 0)
                {
                    throw new ArgumentException("Order quantity must be greater than 0", nameof(quantity));
                }

                // Get product details
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogDebug("Product validation failed: Product not found. ProductId: {ProductId}", productId);
                    return new ProductOrderValidation
                    {
                        IsValid = false,
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = 0,
                        ValidationErrors = new List<string> { "Product not found" },
                        ValidationTimestamp = DateTime.UtcNow
                    };
                }

                // Get store details
                var store = await _storeRepository.GetStoreByIdAsync(product.StoreId, cancellationToken);
                if (store == null)
                {
                    _logger.LogDebug("Product validation failed: Store not found. ProductId: {ProductId}, StoreId: {StoreId}", 
                        productId, product.StoreId);
                    return new ProductOrderValidation
                    {
                        IsValid = false,
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = product.StockQuantity,
                        ValidationErrors = new List<string> { "Product store not found" },
                        ValidationTimestamp = DateTime.UtcNow
                    };
                }

                var validationErrors = new List<string>();

                // Check if store is active
                if (!store.IsActive)
                {
                    validationErrors.Add("Product store is inactive");
                }

                // Check if product has sufficient stock
                if (product.StockQuantity < quantity)
                {
                    validationErrors.Add($"Insufficient stock. Available: {product.StockQuantity}, Requested: {quantity}");
                }

                // Check if product price is valid
                if (product.Price <= 0)
                {
                    validationErrors.Add("Product price is invalid");
                }

                var isValid = validationErrors.Count == 0;

                var validation = new ProductOrderValidation
                {
                    IsValid = isValid,
                    ProductId = productId,
                    ProductName = product.Name ?? "Unknown",
                    StoreId = product.StoreId,
                    StoreName = store.Name ?? "Unknown",
                    RequestedQuantity = quantity,
                    AvailableQuantity = product.StockQuantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * quantity,
                    ValidationErrors = validationErrors,
                    ValidationTimestamp = DateTime.UtcNow
                };

                _logger.LogDebug("Product validation completed: ProductId: {ProductId}, IsValid: {IsValid}, Errors: {ErrorCount}", 
                    productId, isValid, validationErrors.Count);

                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate product for order: ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);
                throw;
            }
        }
    }
}
