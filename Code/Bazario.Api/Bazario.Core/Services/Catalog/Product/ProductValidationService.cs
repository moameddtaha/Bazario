using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums.Order;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.Models.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Product
{
    /// <summary>
    /// Service implementation for product validation operations
    /// </summary>
    public class ProductValidationService : IProductValidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductValidationService> _logger;
        private readonly IConfigurationHelper _configHelper;
        private readonly decimal _maximumProductPrice;
        private readonly int _maximumOrderQuantity;
        private readonly int _reservationLookbackDays;
        private readonly decimal _maximumOrderTotal;

        // Default configuration values
        private const int DEFAULT_RESERVATION_LOOKBACK_DAYS = 7; // Based on typical payment processing window
        private const int MAX_VALIDATION_ERRORS = 6; // Maximum possible validation errors in ValidateForOrderAsync

        // Configuration keys
        private static class ConfigurationKeys
        {
            public const string MaximumProductPrice = "Validation:MaximumProductPrice";
            public const string MaximumOrderQuantity = "Validation:MaximumOrderQuantity";
            public const string ReservationLookbackDays = "Validation:ReservationLookbackDays";
            public const string MaximumOrderTotal = "Validation:MaximumOrderTotal";
        }

        // Error message constants for consistency and potential localization
        private static class ErrorMessages
        {
            public const string ProductNotFound = "Product not found";
            public const string ProductDeleted = "Product has been deleted";
            public const string StoreNotFound = "Product store not found";
            public const string StoreDeleted = "Product store has been deleted";
            public const string StoreInactive = "Product store is inactive";
            public const string ProductNameMissing = "Product name is missing";
            public const string InsufficientStock = "Insufficient stock. Available: {0}, Requested: {1}";
            public const string PriceMustBePositive = "Product price must be greater than zero";
            public const string PriceExceedsMaximum = "Product price exceeds maximum allowed value of {0}";
            public const string OrderTotalExceedsMaximum = "Order total exceeds maximum allowed value of {0}";
            public const string OrderTotalOverflow = "Order total calculation resulted in overflow";
        }

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
            IConfigurationHelper configHelper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configHelper = configHelper ?? throw new ArgumentNullException(nameof(configHelper));

            // Load configurable thresholds with defaults (aligned with InventoryAlertService pattern)
            _maximumProductPrice = _configHelper.GetValue(ConfigurationKeys.MaximumProductPrice, 1_000_000m);
            _maximumOrderQuantity = _configHelper.GetValue(ConfigurationKeys.MaximumOrderQuantity, 10_000);
            _reservationLookbackDays = _configHelper.GetValue(ConfigurationKeys.ReservationLookbackDays, DEFAULT_RESERVATION_LOOKBACK_DAYS);
            _maximumOrderTotal = _configHelper.GetValue(ConfigurationKeys.MaximumOrderTotal, 10_000_000m);
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
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = 0,
                        ValidationErrors = new List<string> { product == null ? ErrorMessages.ProductNotFound : ErrorMessages.ProductDeleted },
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
                        ProductId = productId,
                        RequestedQuantity = quantity,
                        AvailableQuantity = product.StockQuantity,
                        ValidationErrors = new List<string> { store == null ? ErrorMessages.StoreNotFound : ErrorMessages.StoreDeleted },
                        ValidationTimestamp = DateTime.UtcNow
                    };
                }

                var validationErrors = new List<string>(capacity: MAX_VALIDATION_ERRORS); // Pre-allocate for maximum possible validation errors

                // Check for cancellation before starting validation logic
                cancellationToken.ThrowIfCancellationRequested();

                // Business Rule 1: Store must be active for orders to be placed
                // Inactive stores cannot process orders (business decision to prevent orders during maintenance/suspension)
                if (!store.IsActive)
                {
                    validationErrors.Add(ErrorMessages.StoreInactive);
                }

                // Business Rule 2: Product must have a valid name for order processing
                // This ensures proper order display and fulfillment tracking
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    validationErrors.Add(ErrorMessages.ProductNameMissing);
                }

                // Business Rule 3: Stock availability check
                // =====================================================================================
                // ARCHITECTURAL DECISION: Race Condition Documented and Accepted for Pre-Validation
                // =====================================================================================
                // WARNING: This validation is NOT atomic and is susceptible to race conditions.
                // Multiple concurrent requests can all pass this check simultaneously, leading to overselling.
                //
                // DESIGN INTENT: This service is intentionally designed for PRE-VALIDATION ONLY.
                // Actual stock reservation and concurrency control MUST be handled by the order processing layer.
                //
                // Recommended implementation in order processing:
                // 1. Use IInventoryManagementService.CreateStockReservationAsync() for pessimistic locking
                // 2. Product entity already has RowVersion - use optimistic concurrency control in order creation
                // 3. Use database row-level locking: WITH (UPDLOCK, ROWLOCK) in SQL queries
                //
                // This service should NEVER be used as the final authority for stock commitment.
                // It serves as an early check to improve UX by catching obvious stock issues before order submission.
                // =====================================================================================
                if (product.StockQuantity < quantity)
                {
                    validationErrors.Add(string.Format(ErrorMessages.InsufficientStock, product.StockQuantity, quantity));
                }

                // Business Rule 4: Price validation with configurable limits
                // Prevents invalid prices (zero/negative) and unreasonably high prices (potential fraud/data errors)
                if (product.Price <= 0)
                {
                    validationErrors.Add(ErrorMessages.PriceMustBePositive);
                }
                else if (product.Price > _maximumProductPrice)
                {
                    validationErrors.Add(string.Format(ErrorMessages.PriceExceedsMaximum, _maximumProductPrice.ToString("N0", CultureInfo.InvariantCulture)));
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
                        validationErrors.Add(string.Format(ErrorMessages.OrderTotalExceedsMaximum, _maximumOrderTotal.ToString("N0", CultureInfo.InvariantCulture)));
                    }
                }
                catch (OverflowException)
                {
                    validationErrors.Add(ErrorMessages.OrderTotalOverflow);
                    totalPrice = 0; // Safe default for overflow cases
                }

                var validation = new ProductOrderValidation
                {
                    ProductId = productId,
                    ProductName = product.Name ?? "Unknown Product", // Defensive - validation adds error but doesn't prevent assignment
                    StoreId = product.StoreId,
                    StoreName = store.Name ?? "Unknown", // Store name not validated, keep defensive coalescing
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
                _logger.LogDebug(
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

        /// <summary>
        /// Determines whether a product can be safely deleted without violating referential integrity.
        /// Checks for active orders, pending reservations, and reviews.
        /// </summary>
        /// <param name="productId">The ID of the product to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// True if the product can be safely deleted (no active orders, no pending reservations).
        /// False if deletion would violate business rules or on error (fail-safe to prevent deletion).
        /// Note: Having reviews does not prevent deletion but logs a warning suggesting soft delete.
        /// </returns>
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
                    _logger.LogWarning("Product {ProductId} has reviews - consider soft delete instead", productId);
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

        /// <summary>
        /// Checks whether a product has any active orders (Pending, Processing, or Shipped status).
        /// </summary>
        /// <param name="productId">The ID of the product to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// True if the product has active orders.
        /// False if no active orders exist.
        /// On error: Returns true (fail-safe to prevent deletion).
        /// </returns>
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

                // Check for cancellation before database call
                cancellationToken.ThrowIfCancellationRequested();

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

        /// <summary>
        /// Checks whether a product has pending reservations (Pending or Processing orders within lookback period).
        /// Uses configurable lookback period (default: 7 days) to account for payment processing windows.
        /// </summary>
        /// <param name="productId">The ID of the product to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// True if the product has recent pending/processing orders.
        /// False if no pending reservations exist.
        /// On error: Returns true (fail-safe to prevent deletion).
        /// </returns>
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

                // Check for cancellation before database call
                cancellationToken.ThrowIfCancellationRequested();

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

        /// <summary>
        /// Checks whether a product has any customer reviews.
        /// Note: This is informational only and does not block deletion.
        /// </summary>
        /// <param name="productId">The ID of the product to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// True if the product has reviews.
        /// False if no reviews exist.
        /// On error: Returns false (fail-safe to allow operation to proceed, as reviews are not critical for deletion safety).
        /// </returns>
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

                // Check for cancellation before database call
                cancellationToken.ThrowIfCancellationRequested();

                // Get the actual review count from the Reviews repository
                var reviewCount = await _unitOfWork.Reviews.GetReviewCountByProductIdAsync(productId, cancellationToken);

                var hasReviews = reviewCount > 0;

                if (hasReviews)
                {
                    _logger.LogWarning("Product {ProductId} has {ReviewCount} reviews - consider soft delete", productId, reviewCount);
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
