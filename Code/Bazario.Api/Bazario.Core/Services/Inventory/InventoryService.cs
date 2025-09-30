using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Composite service for inventory management operations
    /// Delegates to specialized services following SOLID principles
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryManagementService _managementService;
        private readonly IInventoryQueryService _queryService;
        private readonly IInventoryValidationService _validationService;
        private readonly IInventoryAnalyticsService _analyticsService;
        private readonly IInventoryAlertService _alertService;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            IInventoryManagementService managementService,
            IInventoryQueryService queryService,
            IInventoryValidationService validationService,
            IInventoryAnalyticsService analyticsService,
            IInventoryAlertService alertService,
            ILogger<InventoryService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // IInventoryManagementService methods
        public Task<InventoryUpdateResult> UpdateStockAsync(Guid productId, int newQuantity, StockUpdateType updateType, string reason, Guid updatedBy, CancellationToken cancellationToken = default)
            => _managementService.UpdateStockAsync(productId, newQuantity, updateType, reason, updatedBy, cancellationToken);

        public Task<StockReservationResult> ReserveStockAsync(StockReservationRequest reservationRequest, CancellationToken cancellationToken = default)
            => _managementService.ReserveStockAsync(reservationRequest, cancellationToken);

        public Task<bool> ReleaseReservationAsync(Guid reservationId, string reason, CancellationToken cancellationToken = default)
            => _managementService.ReleaseReservationAsync(reservationId, reason, cancellationToken);

        public Task<bool> ConfirmReservationAsync(Guid reservationId, Guid orderId, CancellationToken cancellationToken = default)
            => _managementService.ConfirmReservationAsync(reservationId, orderId, cancellationToken);

        public Task<BulkInventoryUpdateResult> BulkUpdateStockAsync(BulkStockUpdateRequest bulkUpdateRequest, CancellationToken cancellationToken = default)
            => _managementService.BulkUpdateStockAsync(bulkUpdateRequest, cancellationToken);

        public Task<bool> SetLowStockThresholdAsync(Guid productId, int threshold, CancellationToken cancellationToken = default)
            => _managementService.SetLowStockThresholdAsync(productId, threshold, cancellationToken);

        public Task<int> CleanupExpiredReservationsAsync(int expirationMinutes = 30, CancellationToken cancellationToken = default)
            => _managementService.CleanupExpiredReservationsAsync(expirationMinutes, cancellationToken);

        // IInventoryQueryService methods
        public Task<InventoryStatus> GetInventoryStatusAsync(Guid productId, CancellationToken cancellationToken = default)
            => _queryService.GetInventoryStatusAsync(productId, cancellationToken);

        public Task<List<LowStockAlert>> GetLowStockAlertsAsync(Guid? storeId = null, int threshold = 10, CancellationToken cancellationToken = default)
            => _queryService.GetLowStockAlertsAsync(storeId, threshold, cancellationToken);

        public Task<List<InventoryMovement>> GetInventoryHistoryAsync(Guid productId, DateRange? dateRange = null, CancellationToken cancellationToken = default)
            => _queryService.GetInventoryHistoryAsync(productId, dateRange, cancellationToken);

        public Task<List<StockReservation>> GetActiveReservationsAsync(Guid? productId = null, Guid? storeId = null, CancellationToken cancellationToken = default)
            => _queryService.GetActiveReservationsAsync(productId, storeId, cancellationToken);

        public Task<StockReservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
            => _queryService.GetReservationByIdAsync(reservationId, cancellationToken);

        public Task<List<InventoryStatus>> GetInventoryStatusBulkAsync(List<Guid> productIds, CancellationToken cancellationToken = default)
            => _queryService.GetInventoryStatusBulkAsync(productIds, cancellationToken);

        // IInventoryValidationService methods
        public Task<List<StockValidationResult>> ValidateStockAvailabilityAsync(List<StockCheckItem> stockCheckRequest, CancellationToken cancellationToken = default)
            => _validationService.ValidateStockAvailabilityAsync(stockCheckRequest, cancellationToken);

        public Task<bool> ValidateStockUpdateAsync(Guid productId, int newQuantity, StockUpdateType updateType, CancellationToken cancellationToken = default)
            => _validationService.ValidateStockUpdateAsync(productId, newQuantity, updateType, cancellationToken);

        public Task<bool> ValidateReservationAsync(StockReservationRequest reservationRequest, CancellationToken cancellationToken = default)
            => _validationService.ValidateReservationAsync(reservationRequest, cancellationToken);

        public Task<bool> HasSufficientStockAsync(Guid productId, int requiredQuantity, CancellationToken cancellationToken = default)
            => _validationService.HasSufficientStockAsync(productId, requiredQuantity, cancellationToken);

        public Task<List<BulkUpdateError>> ValidateBulkUpdateAsync(BulkStockUpdateRequest bulkUpdateRequest, CancellationToken cancellationToken = default)
            => _validationService.ValidateBulkUpdateAsync(bulkUpdateRequest, cancellationToken);

        public Task<bool> ShouldTriggerLowStockAlertAsync(Guid productId, CancellationToken cancellationToken = default)
            => _validationService.ShouldTriggerLowStockAlertAsync(productId, cancellationToken);

        // IInventoryAnalyticsService methods
        public Task<InventoryReport> GenerateInventoryReportAsync(InventoryReportRequest reportRequest, CancellationToken cancellationToken = default)
            => _analyticsService.GenerateInventoryReportAsync(reportRequest, cancellationToken);

        public Task<List<InventoryTurnoverData>> GetInventoryTurnoverAsync(Guid? storeId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetInventoryTurnoverAsync(storeId, startDate, endDate, cancellationToken);

        public Task<StockValuationReport> GetStockValuationAsync(Guid storeId, CancellationToken cancellationToken = default)
            => _analyticsService.GetStockValuationAsync(storeId, cancellationToken);

        public Task<InventoryPerformanceMetrics> GetInventoryPerformanceMetricsAsync(Guid storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
            => _analyticsService.GetInventoryPerformanceMetricsAsync(storeId, startDate, endDate, cancellationToken);

        public Task<List<StockForecast>> ForecastStockNeedsAsync(Guid storeId, int forecastDays = 30, CancellationToken cancellationToken = default)
            => _analyticsService.ForecastStockNeedsAsync(storeId, forecastDays, cancellationToken);

        public Task<List<DeadStockItem>> GetDeadStockAnalysisAsync(Guid storeId, int daysSinceLastSale = 90, CancellationToken cancellationToken = default)
            => _analyticsService.GetDeadStockAnalysisAsync(storeId, daysSinceLastSale, cancellationToken);

        // IInventoryAlertService methods
        public Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken cancellationToken = default)
            => _alertService.SendLowStockAlertAsync(productId, currentStock, threshold, cancellationToken);

        public Task SendOutOfStockNotificationAsync(Guid productId, CancellationToken cancellationToken = default)
            => _alertService.SendOutOfStockNotificationAsync(productId, cancellationToken);

        public Task SendBulkLowStockAlertsAsync(List<LowStockAlert> alerts, CancellationToken cancellationToken = default)
            => _alertService.SendBulkLowStockAlertsAsync(alerts, cancellationToken);

        public Task SendRestockRecommendationAsync(Guid productId, int recommendedQuantity, string reason, CancellationToken cancellationToken = default)
            => _alertService.SendRestockRecommendationAsync(productId, recommendedQuantity, reason, cancellationToken);

        public Task<int> ProcessPendingAlertsAsync(CancellationToken cancellationToken = default)
            => _alertService.ProcessPendingAlertsAsync(cancellationToken);

        public Task<bool> ConfigureAlertPreferencesAsync(Guid storeId, InventoryAlertPreferences preferences, CancellationToken cancellationToken = default)
            => _alertService.ConfigureAlertPreferencesAsync(storeId, preferences, cancellationToken);
    }
}
