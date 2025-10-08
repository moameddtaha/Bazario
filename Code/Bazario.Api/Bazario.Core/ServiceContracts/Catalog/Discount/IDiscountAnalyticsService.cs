using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Catalog.Discount;

namespace Bazario.Core.ServiceContracts.Catalog.Discount
{
    /// <summary>
    /// Service contract for discount analytics and performance tracking.
    /// Provides insights into discount usage, performance, and revenue impact.
    /// </summary>
    public interface IDiscountAnalyticsService
    {
        /// <summary>
        /// Gets usage statistics for a specific discount code.
        /// </summary>
        Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets usage statistics for all discounts within a date range.
        /// </summary>
        Task<List<DiscountUsageStats>> GetAllDiscountUsageStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics for a specific discount code.
        /// </summary>
        Task<DiscountPerformance?> GetDiscountPerformanceAsync(
            string discountCode,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics for all discounts.
        /// </summary>
        Task<List<DiscountPerformance>> GetAllDiscountPerformanceAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets revenue impact analysis for all discounts.
        /// </summary>
        Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top performing discount codes by revenue.
        /// </summary>
        Task<List<DiscountPerformance>> GetTopPerformingDiscountsAsync(
            int topCount = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discount usage statistics for a specific store.
        /// </summary>
        Task<List<DiscountUsageStats>> GetStoreDiscountUsageStatsAsync(
            Guid storeId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets overall discount usage statistics (total created, used, active).
        /// </summary>
        Task<(int TotalCreated, int TotalUsed, int TotalActive)> GetOverallDiscountStatsAsync(CancellationToken cancellationToken = default);
    }
}
