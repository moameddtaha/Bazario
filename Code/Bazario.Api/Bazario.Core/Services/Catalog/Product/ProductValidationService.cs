using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums.Order;
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
                if (product == null || product.IsDeleted)
                {
                    _logger.LogDebug("Product validation failed: Product not found or deleted. ProductId: {ProductId}", productId);
                    return new ProductOrderValidation
                    {
                        IsValid = false,
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = 0,
                        ValidationErrors = new List<string> { product == null ? "Product not found" : "Product has been deleted" },
                        ValidationTimestamp = DateTime.UtcNow
                    };
                }

                // Get store details
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(product.StoreId, cancellationToken);
                if (store == null || store.IsDeleted)
                {
                    _logger.LogDebug("Product validation failed: Store not found or deleted. ProductId: {ProductId}, StoreId: {StoreId}",
                        productId, product.StoreId);
                    return new ProductOrderValidation
                    {
                        IsValid = false,
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = product.StockQuantity,
                        ValidationErrors = new List<string> { store == null ? "Product store not found" : "Product store has been deleted" },
                        ValidationTimestamp = DateTime.UtcNow
                    };
                }

                var validationErrors = new List<string>();

                // Check if store is active
                if (!store.IsActive)
                {
                    validationErrors.Add("Product store is inactive");
                }

                // Validate product name
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    validationErrors.Add("Product name is missing");
                }

                // Check if product has sufficient stock
                if (product.StockQuantity < quantity)
                {
                    validationErrors.Add($"Insufficient stock. Available: {product.StockQuantity}, Requested: {quantity}");
                }

                // Check if product price is valid
                if (product.Price <= 0)
                {
                    validationErrors.Add("Product price must be greater than zero");
                }
                else if (product.Price > 1000000) // Maximum reasonable price (configurable in production)
                {
                    validationErrors.Add("Product price exceeds maximum allowed value");
                }

                // Validate total price calculation for overflow
                try
                {
                    var totalPrice = checked(product.Price * quantity);
                    if (totalPrice > decimal.MaxValue / 2) // Reasonable threshold
                    {
                        validationErrors.Add("Order total exceeds maximum allowed value");
                    }
                }
                catch (OverflowException)
                {
                    validationErrors.Add("Order total calculation resulted in overflow");
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
                    ValidationTimestamp = DateTime.UtcNow,
                    IsInStock = product.StockQuantity > 0,
                    IsActive = store.IsActive
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
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("CanProductBeSafelyDeletedAsync called with empty product ID");
                return false; // Can't safely delete an invalid product
            }

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
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("HasProductActiveOrdersAsync called with empty product ID");
                return false;
            }

            try
            {
                _logger.LogDebug("Checking active orders for product {ProductId}", productId);

                // Define active order statuses
                var activeStatuses = new[]
                {
                    OrderStatus.Pending.ToString(),
                    OrderStatus.Processing.ToString(),
                    OrderStatus.Shipped.ToString()
                };

                // Use database-level filtering to avoid loading all orders into memory
                var activeOrders = await _unitOfWork.Orders.GetFilteredOrdersAsync(
                    o => activeStatuses.Contains(o.Status) &&
                         o.OrderItems != null &&
                         o.OrderItems.Any(oi => oi.ProductId == productId),
                    cancellationToken);

                var hasActiveOrders = activeOrders.Any();

                if (hasActiveOrders)
                {
                    _logger.LogInformation("Product {ProductId} has {Count} active orders", productId, activeOrders.Count);
                }
                else
                {
                    _logger.LogDebug("No active orders found for product {ProductId}", productId);
                }

                return hasActiveOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active orders for product {ProductId}", productId);
                return true; // Fail-safe: assume it has active orders to prevent deletion
            }
        }

        public async Task<bool> HasProductPendingReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("HasProductPendingReservationsAsync called with empty product ID");
                return false;
            }

            try
            {
                _logger.LogDebug("Checking pending reservations for product {ProductId}", productId);

                // Define reservation statuses (pending/processing orders in the last 7 days)
                var reservationStatuses = new[]
                {
                    OrderStatus.Pending.ToString(),
                    OrderStatus.Processing.ToString()
                };

                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

                // Use database-level filtering to avoid loading all orders into memory
                var recentOrders = await _unitOfWork.Orders.GetFilteredOrdersAsync(
                    o => o.Date > sevenDaysAgo &&
                         reservationStatuses.Contains(o.Status) &&
                         o.OrderItems != null &&
                         o.OrderItems.Any(oi => oi.ProductId == productId),
                    cancellationToken);

                var hasActiveReservations = recentOrders.Count > 0;

                if (hasActiveReservations)
                {
                    _logger.LogWarning("Product {ProductId} has {Count} recent orders that might indicate active reservations",
                        productId, recentOrders.Count);
                }
                else
                {
                    _logger.LogDebug("No pending reservations found for product {ProductId}", productId);
                }

                return hasActiveReservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending reservations for product {ProductId}", productId);
                return true; // Fail-safe: assume it has reservations to prevent deletion
            }
        }

        public async Task<bool> HasProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("HasProductReviewsAsync called with empty product ID");
                return false;
            }

            try
            {
                _logger.LogDebug("Checking reviews for product {ProductId}", productId);

                // Get the actual review count from the Reviews repository
                var reviewCount = await _unitOfWork.Reviews.GetReviewCountByProductIdAsync(productId, cancellationToken);

                var hasReviews = reviewCount > 0;

                if (hasReviews)
                {
                    _logger.LogInformation("Product {ProductId} has {ReviewCount} reviews", productId, reviewCount);
                }
                else
                {
                    _logger.LogDebug("No reviews found for product {ProductId}", productId);
                }

                return hasReviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reviews for product {ProductId}", productId);
                return false; // Fail-safe: assume no reviews on error (allows operation to proceed)
            }
        }
    }
}
