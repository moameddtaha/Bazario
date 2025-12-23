using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Review;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Review
{
    /// <summary>
    /// Service for review analytics and statistics
    /// </summary>
    public class ReviewAnalyticsService : IReviewAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewAnalyticsService> _logger;

        public ReviewAnalyticsService(
            IUnitOfWork unitOfWork,
            ILogger<ReviewAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets comprehensive rating statistics for a product
        /// </summary>
        public async Task<ProductRatingStats> GetProductRatingStatsAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("GetProductRatingStatsAsync called with empty product ID");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            _logger.LogDebug("Getting rating statistics for product {ProductId}", productId);

            var reviews = await _unitOfWork.Reviews.GetReviewsByProductIdAsync(productId, cancellationToken);

            if (reviews == null || !reviews.Any())
            {
                _logger.LogDebug("No reviews found for product {ProductId}, returning empty statistics", productId);
                return new ProductRatingStats
                {
                    ProductId = productId,
                    AverageRating = 0,
                    TotalReviews = 0,
                    FiveStarCount = 0,
                    FourStarCount = 0,
                    ThreeStarCount = 0,
                    TwoStarCount = 0,
                    OneStarCount = 0,
                    FiveStarPercentage = 0,
                    FourStarPercentage = 0,
                    ThreeStarPercentage = 0,
                    TwoStarPercentage = 0,
                    OneStarPercentage = 0,
                    RatingDistribution = new System.Collections.Generic.Dictionary<int, int>
                    {
                        { 1, 0 },
                        { 2, 0 },
                        { 3, 0 },
                        { 4, 0 },
                        { 5, 0 }
                    }
                };
            }

            var totalReviews = reviews.Count;
            var fiveStarCount = reviews.Count(r => r.Rating == 5);
            var fourStarCount = reviews.Count(r => r.Rating == 4);
            var threeStarCount = reviews.Count(r => r.Rating == 3);
            var twoStarCount = reviews.Count(r => r.Rating == 2);
            var oneStarCount = reviews.Count(r => r.Rating == 1);

            var stats = new ProductRatingStats
            {
                ProductId = productId,
                AverageRating = (decimal)reviews.Average(r => r.Rating),
                TotalReviews = totalReviews,
                FiveStarCount = fiveStarCount,
                FourStarCount = fourStarCount,
                ThreeStarCount = threeStarCount,
                TwoStarCount = twoStarCount,
                OneStarCount = oneStarCount,
                FiveStarPercentage = (double)fiveStarCount / totalReviews * 100,
                FourStarPercentage = (double)fourStarCount / totalReviews * 100,
                ThreeStarPercentage = (double)threeStarCount / totalReviews * 100,
                TwoStarPercentage = (double)twoStarCount / totalReviews * 100,
                OneStarPercentage = (double)oneStarCount / totalReviews * 100,
                RatingDistribution = new System.Collections.Generic.Dictionary<int, int>
                {
                    { 1, oneStarCount },
                    { 2, twoStarCount },
                    { 3, threeStarCount },
                    { 4, fourStarCount },
                    { 5, fiveStarCount }
                }
            };

            _logger.LogDebug(
                "Product {ProductId} rating statistics: {Average} average from {Total} reviews",
                productId, stats.AverageRating, stats.TotalReviews);

            return stats;
        }
    }
}
