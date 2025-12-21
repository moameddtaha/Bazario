using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Models.Catalog.Discount;
using Bazario.Core.ServiceContracts.Catalog.Discount;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Composite service for all discount-related operations.
    /// Delegates to specialized services for management, validation, and analytics.
    /// Uses DTOs for request/response to provide validation, security, and versioning.
    /// </summary>
    public class DiscountService : IDiscountService
    {
        private readonly IDiscountManagementService _managementService;
        private readonly IDiscountValidationService _validationService;
        private readonly IDiscountAnalyticsService _analyticsService;

        public DiscountService(
            IDiscountManagementService managementService,
            IDiscountValidationService validationService,
            IDiscountAnalyticsService analyticsService)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        // IDiscountManagementService methods - now using DTOs
        public Task<DiscountResponse> CreateDiscountAsync(DiscountAddRequest request, CancellationToken cancellationToken = default)
            => _managementService.CreateDiscountAsync(request, cancellationToken);

        public Task<DiscountResponse> UpdateDiscountAsync(DiscountUpdateRequest request, CancellationToken cancellationToken = default)
            => _managementService.UpdateDiscountAsync(request, cancellationToken);

        public Task<bool> DeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default)
            => _managementService.DeleteDiscountAsync(discountId, cancellationToken);

        public Task<DiscountResponse?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default)
            => _managementService.GetDiscountByIdAsync(discountId, cancellationToken);

        public Task<DiscountResponse?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default)
            => _managementService.GetDiscountByCodeAsync(code, cancellationToken);

        public Task<List<DiscountResponse>> GetStoreDiscountsAsync(Guid storeId, CancellationToken cancellationToken = default)
            => _managementService.GetStoreDiscountsAsync(storeId, cancellationToken);

        public Task<List<DiscountResponse>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default)
            => _managementService.GetGlobalDiscountsAsync(cancellationToken);

        public Task<List<DiscountResponse>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default)
            => _managementService.GetDiscountsByTypeAsync(type, cancellationToken);

        public Task<List<DiscountResponse>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
            => _managementService.GetExpiringDiscountsAsync(daysUntilExpiry, cancellationToken);

        public Task<List<DiscountResponse>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default)
            => _managementService.GetActiveDiscountsAsync(cancellationToken);

        // IDiscountValidationService methods - now using DTOs
        public Task<(bool IsValid, DiscountResponse? Discount, string? ErrorMessage)> ValidateDiscountCodeAsync(string code, decimal orderSubtotal, List<Guid> storeIds, CancellationToken cancellationToken = default)
            => _validationService.ValidateDiscountCodeAsync(code, orderSubtotal, storeIds, cancellationToken);

        public Task<(List<DiscountResponse> ValidDiscounts, List<string> ErrorMessages)> ValidateMultipleDiscountCodesAsync(List<string> codes, decimal orderSubtotal, List<Guid> storeIds, CancellationToken cancellationToken = default)
            => _validationService.ValidateMultipleDiscountCodesAsync(codes, orderSubtotal, storeIds, cancellationToken);

        public Task<bool> DiscountExistsAsync(string code, CancellationToken cancellationToken = default)
            => _validationService.DiscountExistsAsync(code, cancellationToken);

        public Task<bool> IsDiscountCodeUniqueAsync(string code, Guid? excludeDiscountId = null, CancellationToken cancellationToken = default)
            => _validationService.IsDiscountCodeUniqueAsync(code, excludeDiscountId, cancellationToken);

        public bool ValidateDiscountValue(DiscountType type, decimal value)
            => _validationService.ValidateDiscountValue(type, value);

        public bool ValidateDateRange(DateTime validFrom, DateTime validTo)
            => _validationService.ValidateDateRange(validFrom, validTo);

        public Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default)
            => _validationService.MarkDiscountAsUsedAsync(discountId, cancellationToken);

        // IDiscountAnalyticsService methods
        public Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default)
            => _analyticsService.GetDiscountUsageStatsAsync(discountCode, cancellationToken);

        public Task<List<DiscountUsageStats>> GetAllDiscountUsageStatsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetAllDiscountUsageStatsAsync(startDate, endDate, cancellationToken);

        public Task<DiscountPerformance?> GetDiscountPerformanceAsync(string discountCode, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetDiscountPerformanceAsync(discountCode, startDate, endDate, cancellationToken);

        public Task<List<DiscountPerformance>> GetAllDiscountPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetAllDiscountPerformanceAsync(startDate, endDate, cancellationToken);

        public Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetDiscountRevenueImpactAsync(startDate, endDate, cancellationToken);

        public Task<List<DiscountPerformance>> GetTopPerformingDiscountsAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetTopPerformingDiscountsAsync(topCount, startDate, endDate, cancellationToken);

        public Task<List<DiscountUsageStats>> GetStoreDiscountUsageStatsAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
            => _analyticsService.GetStoreDiscountUsageStatsAsync(storeId, startDate, endDate, cancellationToken);

        public Task<(int TotalCreated, int TotalUsed, int TotalActive)> GetOverallDiscountStatsAsync(CancellationToken cancellationToken = default)
            => _analyticsService.GetOverallDiscountStatsAsync(cancellationToken);
    }
}
