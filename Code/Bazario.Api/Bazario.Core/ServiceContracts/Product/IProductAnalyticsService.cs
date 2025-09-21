using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Product;

namespace Bazario.Core.ServiceContracts.Product
{
    /// <summary>
    /// Service contract for product analytics operations
    /// Handles product analytics, reporting, and insights
    /// </summary>
    public interface IProductAnalyticsService
    {
        /// <summary>
        /// Gets product analytics including sales, views, ratings
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Product analytics data</returns>
        Task<ProductAnalytics> GetProductAnalyticsAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
