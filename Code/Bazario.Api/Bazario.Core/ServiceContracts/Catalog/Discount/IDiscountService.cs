namespace Bazario.Core.ServiceContracts.Catalog.Discount
{
    /// <summary>
    /// Composite service interface for all discount-related operations.
    /// Combines management, validation, and analytics capabilities.
    /// </summary>
    public interface IDiscountService : IDiscountManagementService, IDiscountValidationService, IDiscountAnalyticsService
    {
        // This interface inherits all methods from:
        // - IDiscountManagementService (CRUD operations)
        // - IDiscountValidationService (validation business rules)
        // - IDiscountAnalyticsService (performance tracking and analytics)
    }
}
