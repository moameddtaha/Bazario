using Bazario.Core.Models.Store;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Helper interface for store query operations
    /// </summary>
    public interface IStoreQueryHelper
    {
        /// <summary>
        /// Applies soft deletion filters to the query based on search criteria
        /// </summary>
        IQueryable<Domain.Entities.Store.Store> ApplySoftDeletionFilters(IQueryable<Domain.Entities.Store.Store> query, StoreSearchCriteria searchCriteria);

        /// <summary>
        /// Applies sorting to the query based on search criteria
        /// </summary>
        IQueryable<Domain.Entities.Store.Store> ApplySorting(IQueryable<Domain.Entities.Store.Store> query, StoreSearchCriteria searchCriteria);
    }
}
