using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product validation operations
    /// Handles product validation logic and business rules including deletion safety
    ///
    /// Fail-Safe Strategy:
    /// - Active orders/reservations: Returns TRUE on error (blocks deletion - safe choice)
    /// - Reviews: Returns FALSE on error (allows deletion - reviews are informational only)
    /// This asymmetry is intentional to protect referential integrity while being permissive for non-critical data
    /// </summary>
    public class ProductValidationService : IProductValidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductValidationService> _logger;
        private readonly decimal _maximumProductPrice;
        private readonly int _maximumOrderQuantity;
        private readonly int _reservationLookbackDays;
        private readonly decimal _maximumOrderTotal;

        // Static arrays for order status filtering (created once, reused for performance)
        private static readonly string[] ActiveOrderStatuses = new[]
        {
            OrderStatus.Pending.ToString(),
            OrderStatus.Processing.ToString(),
            OrderStatus.Shipped.ToString()
        };

        private static readonly string[] ReservationStatuses = new[]
        {
            OrderStatus.Pending.ToString(),
            OrderStatus.Processing.ToString()
        };

        public ProductValidationService(
            IUnitOfWork unitOfWork,
            ILogger<ProductValidationService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load configurable thresholds with defaults (aligned with InventoryAlertService pattern)
            _maximumProductPrice = GetConfigurationValue(configuration, "Validation:MaximumProductPrice", 1_000_000m);
            _maximumOrderQuantity = GetConfigurationValue(configuration, "Validation:MaximumOrderQuantity", 10_000);
            _reservationLookbackDays = GetConfigurationValue(configuration, "Validation:ReservationLookbackDays", 7);
            _maximumOrderTotal = GetConfigurationValue(configuration, "Validation:MaximumOrderTotal", decimal.MaxValue / 2);
        }

        public async Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
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

                if (quantity > _maximumOrderQuantity)
                {
                    throw new ArgumentException($"Order quantity exceeds maximum allowed value of {_maximumOrderQuantity}", nameof(quantity));
                }

                // Get product with store in single query (performance optimization)
                var product = await _unitOfWork.Products.GetProductWithStoreByIdAsync(productId, cancellationToken);
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

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Store is already loaded via single query
                var store = product.Store;
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

                // Business Rule 1: Store must be active for orders to be placed
                // Inactive stores cannot process orders (business decision to prevent orders during maintenance/suspension)
                if (!store.IsActive)
                {
                    validationErrors.Add("Product store is inactive");
                }

                // Business Rule 2: Product must have a valid name for order processing
                // This ensures proper order display and fulfillment tracking
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    validationErrors.Add("Product name is missing");
                }

                // Business Rule 3: Stock availability check
                // NOTE: This is NOT atomic - use with stock reservation system for production
                // Multiple concurrent requests can pass this check but exceed actual stock
                if (product.StockQuantity < quantity)
                {
                    validationErrors.Add($"Insufficient stock. Available: {product.StockQuantity}, Requested: {quantity}");
                }

                // Business Rule 4: Price validation with configurable limits
                // Prevents invalid prices (zero/negative) and unreasonably high prices (potential fraud/data errors)
                if (product.Price <= 0)
                {
                    validationErrors.Add("Product price must be greater than zero");
                }
                else if (product.Price > _maximumProductPrice)
                {
                    validationErrors.Add($"Product price exceeds maximum allowed value of {_maximumProductPrice:N0}");
                }

                // Business Rule 5: Order total overflow protection
                // Uses checked arithmetic to detect overflow and validates against configurable maximum
                // This prevents decimal overflow in payment processing and database storage
                decimal totalPrice;
                try
                {
                    totalPrice = checked(product.Price * quantity);
                    if (totalPrice > _maximumOrderTotal)
                    {
                        validationErrors.Add($"Order total exceeds maximum allowed value of {_maximumOrderTotal:N0}");
                    }
                }
                catch (OverflowException)
                {
                    validationErrors.Add("Order total calculation resulted in overflow");
                    totalPrice = 0; // Safe default for overflow cases
                }

                var validation = new ProductOrderValidation
                {
                    IsValid = validationErrors.Count == 0,
                    ProductId = productId,
                    ProductName = product.Name ?? "Unknown",
                    StoreId = product.StoreId,
                    StoreName = store.Name ?? "Unknown",
                    RequestedQuantity = quantity,
                    AvailableQuantity = product.StockQuantity,
                    UnitPrice = product.Price,
                    TotalPrice = totalPrice, // Use the validated value
                    ValidationErrors = validationErrors,
                    ValidationTimestamp = DateTime.UtcNow,
                    IsInStock = product.StockQuantity > 0,
                    IsActive = store.IsActive
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "Product validation completed in {ElapsedMs}ms: ProductId: {ProductId}, IsValid: {IsValid}, ErrorCount: {ErrorCount}",
                    stopwatch.ElapsedMilliseconds, productId, validation.IsValid, validationErrors.Count);

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

                // Use optimized EXISTS query - no data loaded into memory, just returns boolean
                var hasActiveOrders = await _unitOfWork.Orders.HasProductInOrdersWithStatusAsync(
                    productId,
                    ActiveOrderStatuses,
                    cancellationToken);

                if (hasActiveOrders)
                {
                    _logger.LogWarning("Product {ProductId} has active orders - deletion blocked", productId);
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

                var lookbackDate = DateTime.UtcNow.AddDays(-_reservationLookbackDays);

                // Use optimized EXISTS query with date filtering - no data loaded into memory
                var hasActiveReservations = await _unitOfWork.Orders.HasProductInOrdersWithStatusAndDateAsync(
                    productId,
                    ReservationStatuses,
                    lookbackDate,
                    cancellationToken);

                if (hasActiveReservations)
                {
                    _logger.LogWarning("Product {ProductId} has recent pending/processing orders - deletion blocked", productId);
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
                    _logger.LogInformation("Product {ProductId} has {ReviewCount} reviews - consider soft delete", productId, reviewCount);
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

        /// <summary>
        /// Helper method to safely retrieve configuration values with defaults
        /// </summary>
        private static T GetConfigurationValue<T>(IConfiguration configuration, string key, T defaultValue)
        {
            if (configuration == null)
                return defaultValue;

            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
