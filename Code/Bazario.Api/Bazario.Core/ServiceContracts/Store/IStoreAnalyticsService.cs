using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Enums;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Analytics and performance reporting for stores
    /// </summary>
    public interface IStoreAnalyticsService
    {
        /// <summary>
        /// Gets comprehensive analytics for a store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="dateRange">Date range for analytics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store analytics data</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found</exception>
        Task<StoreAnalytics> GetStoreAnalyticsAsync(Guid storeId, DateRange? dateRange = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets store performance summary
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Store performance data</returns>
        /// <exception cref="InvalidOperationException">Thrown when store not found</exception>
        Task<StorePerformance> GetStorePerformanceAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top performing stores with pagination
        /// </summary>
        /// <param name="criteria">Performance criteria</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Top performing stores</returns>
        Task<PagedResponse<StorePerformance>> GetTopPerformingStoresAsync(PerformanceCriteria criteria = PerformanceCriteria.Revenue, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    }
}
