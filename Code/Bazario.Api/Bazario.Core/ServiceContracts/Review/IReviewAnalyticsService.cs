using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Review;

namespace Bazario.Core.ServiceContracts.Review
{
    /// <summary>
    /// Service for review analytics and statistics
    /// </summary>
    public interface IReviewAnalyticsService
    {
        /// <summary>
        /// Gets comprehensive rating statistics for a product
        /// </summary>
        Task<ProductRatingStats> GetProductRatingStatsAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
