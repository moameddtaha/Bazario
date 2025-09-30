using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Service contract for inventory analytics and reporting
    /// Handles inventory reports, forecasting, and analytics
    /// </summary>
    public interface IInventoryAnalyticsService
    {
        /// <summary>
        /// Generates inventory report for a store or date range
        /// </summary>
        Task<InventoryReport> GenerateInventoryReportAsync(InventoryReportRequest reportRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets inventory turnover rate for products
        /// </summary>
        Task<List<InventoryTurnoverData>> GetInventoryTurnoverAsync(Guid? storeId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stock valuation for a store
        /// </summary>
        Task<StockValuationReport> GetStockValuationAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets inventory performance metrics
        /// </summary>
        Task<InventoryPerformanceMetrics> GetInventoryPerformanceMetricsAsync(Guid storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forecasts stock needs based on historical data
        /// </summary>
        Task<List<StockForecast>> ForecastStockNeedsAsync(Guid storeId, int forecastDays = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dead stock analysis (slow-moving items)
        /// </summary>
        Task<List<DeadStockItem>> GetDeadStockAnalysisAsync(Guid storeId, int daysSinceLastSale = 90, CancellationToken cancellationToken = default);
    }
}
