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

                // Get product analytics efficiently using database aggregation
                // Use last 12 months for product analytics (same as store analytics)
                var oneYearAgo = DateTime.UtcNow.AddYears(-1);
                var salesStats = await _productRepository.GetProductSalesStatsAsync(productId, oneYearAgo, DateTime.UtcNow, cancellationToken);
                
                // Get review statistics efficiently
                var averageRating = await _reviewRepository.GetAverageRatingByProductIdAsync(productId, cancellationToken);
                var reviewCount = await _reviewRepository.GetReviewCountByProductIdAsync(productId, cancellationToken);

                // Convert monthly product sales data to monthly sales data
                var monthlySales = salesStats.MonthlyData.Select(m => new MonthlySalesData
                {
                    Month = $"{m.Year}-{m.Month:D2}",
                    Sales = m.Sales,
                    Year = m.Year,
                    MonthNumber = m.Month,
                    UnitsSold = m.UnitsSold,
                    Revenue = m.Revenue
                }).ToList();

                var analytics = new ProductAnalytics
                {
                    ProductId = productId,
                    ProductName = product.Name ?? "Unknown",
                    TotalSales = salesStats.TotalSales,
                    TotalRevenue = salesStats.TotalRevenue,
                    AverageRating = Math.Round(averageRating, 2),
                    ReviewCount = reviewCount,
                    CurrentStock = product.StockQuantity,
                    MonthlySalesData = monthlySales,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogDebug("Successfully generated analytics for product: {ProductId}, TotalSales: {TotalSales}, TotalRevenue: {TotalRevenue}, AverageRating: {AverageRating}", 
                    productId, salesStats.TotalSales, salesStats.TotalRevenue, averageRating);

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
