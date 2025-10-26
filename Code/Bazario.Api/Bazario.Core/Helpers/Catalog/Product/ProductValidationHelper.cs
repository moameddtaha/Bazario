using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Catalog.Product
{
    /// <summary>
    /// Helper class for product validation operations
    /// </summary>
    public class ProductValidationHelper : IProductValidationHelper
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<ProductValidationHelper> _logger;

        public ProductValidationHelper(
            IOrderRepository orderRepository,
            ILogger<ProductValidationHelper> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var orders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
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
                
                // Implement real reservation checking
                // In a real implementation, this would check a StockReservation table
                
                // Real implementation would:
                // 1. Query StockReservation table for active reservations
                // 2. Check reservation status (Pending, Confirmed, etc.)
                // 3. Verify reservation expiration dates
                // 4. Check if reservations are still valid
                
                // For now, implement a basic check that simulates the real logic
                // This ensures products can be deleted when reservation system is not active
                
                // Simulate checking for reservations by looking at recent order activity
                // In a real implementation, this would be:
                // var reservations = await _stockReservationRepository.GetActiveReservationsByProductIdAsync(productId, cancellationToken);
                // var hasActiveReservations = reservations.Any(r => 
                //     r.Status == "Pending" || r.Status == "Confirmed" && 
                //     r.ExpiresAt > DateTime.UtcNow);
                
                // For now, check if there are any recent orders that might indicate reservations
                var recentOrders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
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
                
                // Implement real review checking
                // In a real implementation, this would check a ReviewRepository
                
                // Real implementation would:
                // 1. Query Review table for reviews associated with this product
                // 2. Check review status (Active, Deleted, etc.)
                // 3. Count total reviews for the product
                // 4. Consider review importance (verified purchases, etc.)
                
                // For now, implement a basic check that simulates the real logic
                // This allows products to be deleted when review system is not active
                
                // Simulate checking for reviews by looking at order history
                // In a real implementation, this would be:
                // var reviews = await _reviewRepository.GetReviewsByProductIdAsync(productId, cancellationToken);
                // var hasActiveReviews = reviews.Any(r => r.Status == "Active" && !r.IsDeleted);
                
                // For now, check if there are any orders that might have generated reviews
                var allOrders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
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
