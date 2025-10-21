using Bazario.Core.Models.Store;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;

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
        IQueryable<StoreEntity> ApplySoftDeletionFilters(IQueryable<StoreEntity> query, StoreSearchCriteria searchCriteria);

        /// <summary>
        /// Applies sorting to the query based on search criteria
        /// </summary>
        IQueryable<StoreEntity> ApplySorting(IQueryable<StoreEntity> query, StoreSearchCriteria searchCriteria);
    }
}
