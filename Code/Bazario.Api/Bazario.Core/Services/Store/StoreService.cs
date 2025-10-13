using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Store;
using Bazario.Core.Enums.Store;
using Bazario.Core.Models.Shared;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Composite service implementation for store operations
    /// Delegates to specialized services following SOLID principles
    /// Use sparingly - prefer specific interfaces (IStoreManagementService, IStoreQueryService, etc.)
    /// </summary>
    public class StoreService : IStoreService
    {
        private readonly IStoreManagementService _managementService;
        private readonly IStoreQueryService _queryService;
        private readonly IStoreAnalyticsService _analyticsService;
        private readonly IStoreValidationService _validationService;
        private readonly ILogger<StoreService> _logger;

        public StoreService(
            IStoreManagementService managementService,
            IStoreQueryService queryService,
            IStoreAnalyticsService analyticsService,
            IStoreValidationService validationService,
            ILogger<StoreService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IStoreManagementService Implementation

        public async Task<StoreResponse> CreateStoreAsync(StoreAddRequest storeAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store creation to management service");
            return await _managementService.CreateStoreAsync(storeAddRequest, cancellationToken);
        }

        public async Task<StoreResponse> UpdateStoreAsync(StoreUpdateRequest storeUpdateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store update to management service");
            return await _managementService.UpdateStoreAsync(storeUpdateRequest, cancellationToken);
        }

        public async Task<bool> DeleteStoreAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store deletion to management service");
            return await _managementService.DeleteStoreAsync(storeId, deletedBy, reason, cancellationToken);
        }

        public async Task<bool> HardDeleteStoreAsync(Guid storeId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating hard store deletion to management service");
            return await _managementService.HardDeleteStoreAsync(storeId, deletedBy, reason, cancellationToken);
        }

        public async Task<StoreResponse> RestoreStoreAsync(Guid storeId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store restoration to management service");
            return await _managementService.RestoreStoreAsync(storeId, restoredBy, cancellationToken);
        }

        public async Task<StoreResponse> UpdateStoreStatusAsync(Guid storeId, Guid userId, bool isActive, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store status update to management service");
            return await _managementService.UpdateStoreStatusAsync(storeId, userId, isActive, reason, cancellationToken);
        }

        #endregion

        #region IStoreQueryService Implementation

        public async Task<StoreResponse?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store retrieval to query service");
            return await _queryService.GetStoreByIdAsync(storeId, cancellationToken);
        }

        public async Task<List<StoreResponse>> GetStoresBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating seller stores retrieval to query service");
            return await _queryService.GetStoresBySellerIdAsync(sellerId, cancellationToken);
        }

        public async Task<PagedResponse<StoreResponse>> SearchStoresAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store search to query service");
            return await _queryService.SearchStoresAsync(searchCriteria, cancellationToken);
        }

        public async Task<PagedResponse<StoreResponse>> GetStoresByCategoryAsync(StoreSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating category stores retrieval to query service");
            return await _queryService.GetStoresByCategoryAsync(searchCriteria, cancellationToken);
        }

        #endregion

        #region IStoreAnalyticsService Implementation

        public async Task<StoreAnalytics> GetStoreAnalyticsAsync(Guid storeId, DateRange? dateRange = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store analytics to analytics service");
            return await _analyticsService.GetStoreAnalyticsAsync(storeId, dateRange, cancellationToken);
        }

        public async Task<StorePerformance> GetStorePerformanceAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store performance to analytics service");
            return await _analyticsService.GetStorePerformanceAsync(storeId, cancellationToken);
        }

        public async Task<PagedResponse<StorePerformance>> GetTopPerformingStoresAsync(PerformanceCriteria performanceCriteria = PerformanceCriteria.Revenue, StoreSearchCriteria? searchCriteria = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating top performing stores to analytics service");
            return await _analyticsService.GetTopPerformingStoresAsync(performanceCriteria, searchCriteria, cancellationToken);
        }

        #endregion

        #region IStoreValidationService Implementation

        public async Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store creation validation to validation service");
            return await _validationService.ValidateStoreCreationAsync(sellerId, storeName, cancellationToken);
        }

        public async Task<StoreValidationResult> ValidateStoreUpdateAsync(Guid sellerId, StoreUpdateRequest updateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store update validation to validation service");
            return await _validationService.ValidateStoreUpdateAsync(sellerId, updateRequest, cancellationToken);
        }

        public async Task<StoreValidationResult> ValidateStoreSoftDeletionAsync(Guid storeId, Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating store soft deletion validation to validation service");
            return await _validationService.ValidateStoreSoftDeletionAsync(storeId, sellerId, cancellationToken);
        }

        #endregion
    }
}