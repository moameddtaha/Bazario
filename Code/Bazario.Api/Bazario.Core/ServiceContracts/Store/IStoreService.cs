namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Composite interface for components that need all store operations
    /// Use sparingly - prefer specific interfaces (IStoreManagementService, IStoreQueryService, etc.)
    /// 
    /// This interface inherits all methods from:
    /// - IStoreManagementService: CRUD operations (Create, Update, Delete, Status)
    /// - IStoreQueryService: Read operations (GetById, Search, Filter)
    /// - IStoreAnalyticsService: Analytics and reporting (Analytics, Performance, TopStores)
    /// - IStoreValidationService: Business rule validation (ValidateCreation)
    /// </summary>
    public interface IStoreService : IStoreManagementService, IStoreQueryService, IStoreAnalyticsService, IStoreValidationService
    {
        // All methods are inherited from the specialized interfaces above
        // This provides a single interface for components that need all store operations
    }
}
