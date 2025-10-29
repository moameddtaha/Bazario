using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product validation operations
    /// Handles product validation logic and business rules including deletion safety
    /// </summary>
    public class ProductValidationService : IProductValidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductValidationService> _logger;

        public ProductValidationService(
            IUnitOfWork unitOfWork,
            ILogger<ProductValidationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
                var product = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
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
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(product.StoreId, cancellationToken);
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

        public async Task<bool> CanProductBeSafelyDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if product has any active orders
                var hasActiveOrders = await HasProductActiveOrdersAsync(productId, cancellationToken);
                if (hasActiveOrders)
                {
                    _logger.LogWarning("Product {ProductId} has active orders and cannot be safely deleted", productId);
                    return false;
                }

                // Check if product has any pending reservations
                var hasPendingReservations = await HasProductPendingReservationsAsync(productId, cancellationToken);
                if (hasPendingReservations)
                {
                    _logger.LogWarning("Product {ProductId} has pending reservations and cannot be safely deleted", productId);
                    return false;
                }

                // Check if product has any reviews (optional - might want to keep for historical data)
                var hasReviews = await HasProductReviewsAsync(productId, cancellationToken);
                if (hasReviews)
                {
                    _logger.LogInformation("Product {ProductId} has reviews - consider soft delete instead", productId);
                    // This is a warning, not a blocker
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if product {ProductId} can be safely deleted", productId);
                return false; // Fail safe - don't allow deletion on error
            }
        }

        public async Task<bool> HasProductActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if product has any orders in pending, processing, or shipped status
                var orders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                var activeOrders = orders.Where(o =>
                    o.Status == "Pending" ||
                    o.Status == "Processing" ||
                    o.Status == "Shipped" ||
                    o.Status == "Confirmed")
                    .ToList();

                foreach (var order in activeOrders)
                {
                    if (order.OrderItems != null)
                    {
                        if (order.OrderItems.Any(oi => oi.ProductId == productId))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active orders for product {ProductId}", productId);
                return true; // Assume it has active orders to be safe
            }
        }

        public async Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Checking pending reservations for product {ProductId}", productId);

                // Check if there are any recent orders that might indicate reservations
                var recentOrders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                var recentProductOrders = recentOrders
                    .Where(o => o.Date > DateTime.UtcNow.AddDays(-7)) // Last 7 days
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => oi.ProductId == productId))
                    .Where(o => o.Status == "Pending" || o.Status == "Processing")
                    .ToList();

                var hasActiveReservations = recentProductOrders.Any();

                if (hasActiveReservations)
                {
                    _logger.LogWarning("Product {ProductId} has recent orders that might indicate active reservations", productId);
                    return true;
                }

                _logger.LogDebug("No pending reservations found for product {ProductId}", productId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending reservations for product {ProductId}", productId);
                return true; // Assume it has reservations to be safe
            }
        }

        public async Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Checking reviews for product {ProductId}", productId);

                // Check if there are any orders that might have generated reviews
                var allOrders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                var productOrders = allOrders
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => oi.ProductId == productId))
                    .Where(o => o.Status == "Delivered") // Only delivered orders can have reviews
                    .ToList();

                // Simulate that delivered orders might have reviews
                var hasPotentialReviews = productOrders.Any();

                if (hasPotentialReviews)
                {
                    _logger.LogInformation("Product {ProductId} has delivered orders that might have generated reviews", productId);
                    return true;
                }

                _logger.LogDebug("No reviews found for product {ProductId}", productId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reviews for product {ProductId}", productId);
                return false; // Assume no reviews on error
            }
        }
    }
}
