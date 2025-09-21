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
    /// Service implementation for product analytics operations
    /// Handles product analytics, reporting, and insights
    /// </summary>
    public class ProductAnalyticsService : IProductAnalyticsService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<ProductAnalyticsService> _logger;

        public ProductAnalyticsService(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IReviewRepository reviewRepository,
            ILogger<ProductAnalyticsService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductAnalytics> GetProductAnalyticsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Generating analytics for product: {ProductId}", productId);

            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    throw new ArgumentException("Product ID cannot be empty", nameof(productId));
                }

                // Get product details
                var product = await _productRepository.GetProductByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Analytics generation failed: Product not found. ProductId: {ProductId}", productId);
                    throw new InvalidOperationException($"Product with ID {productId} not found");
                }

                // Get orders containing this product
                var orders = await _orderRepository.GetFilteredOrdersAsync(
                    o => o.OrderItems != null && o.OrderItems.Any(oi => oi.ProductId == productId), 
                    cancellationToken);

                // Get reviews for this product
                var reviews = await _reviewRepository.GetReviewsByProductIdAsync(productId, cancellationToken);

                // Calculate analytics
                var totalSales = orders.Sum(o => o.OrderItems?.Where(oi => oi.ProductId == productId).Sum(oi => oi.Quantity) ?? 0);
                var totalRevenue = orders.Sum(o => o.OrderItems?.Where(oi => oi.ProductId == productId).Sum(oi => oi.Quantity * oi.Price) ?? 0);
                var averageRating = reviews.Any() ? (decimal)reviews.Average(r => r.Rating) : 0;
                var reviewCount = reviews.Count;

                // Calculate monthly sales data (last 12 months)
                var monthlySales = new List<MonthlySalesData>();
                var currentDate = DateTime.UtcNow;
                
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    
                    var monthOrders = orders.Where(o => o.Date >= monthStart && o.Date <= monthEnd);
                    var monthSales = monthOrders.Sum(o => o.OrderItems?.Where(oi => oi.ProductId == productId).Sum(oi => oi.Quantity) ?? 0);
                    
                    monthlySales.Add(new MonthlySalesData
                    {
                        Month = monthStart.ToString("yyyy-MM"),
                        Sales = monthSales,
                        Year = monthStart.Year,
                        MonthNumber = monthStart.Month,
                        UnitsSold = monthSales,
                        Revenue = monthOrders.Sum(o => o.OrderItems?.Where(oi => oi.ProductId == productId).Sum(oi => oi.Quantity * oi.Price) ?? 0)
                    });
                }

                var analytics = new ProductAnalytics
                {
                    ProductId = productId,
                    ProductName = product.Name ?? "Unknown",
                    TotalSales = totalSales,
                    TotalRevenue = totalRevenue,
                    AverageRating = Math.Round(averageRating, 2),
                    ReviewCount = reviewCount,
                    CurrentStock = product.StockQuantity,
                    MonthlySalesData = monthlySales,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogDebug("Successfully generated analytics for product: {ProductId}, TotalSales: {TotalSales}, TotalRevenue: {TotalRevenue}, AverageRating: {AverageRating}", 
                    productId, totalSales, totalRevenue, averageRating);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate analytics for product: {ProductId}", productId);
                throw;
            }
        }
    }
}
