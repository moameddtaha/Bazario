using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Helper implementation for store query operations
    /// </summary>
    public class StoreQueryHelper : IStoreQueryHelper
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoreQueryHelper(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public IQueryable<Domain.Entities.Store.Store> ApplySoftDeletionFilters(IQueryable<Domain.Entities.Store.Store> query, StoreSearchCriteria searchCriteria)
        {
            if (searchCriteria.OnlyDeleted)
            {
                // Need to ignore the global filter to get deleted stores
                return _unitOfWork.Stores.GetStoresQueryableIgnoreFilters().Where(s => s.IsDeleted);
            }
            else if (searchCriteria.IncludeDeleted)
            {
                // Need to ignore the global filter to include both active and deleted stores
                return _unitOfWork.Stores.GetStoresQueryableIgnoreFilters();
            }

            // If neither OnlyDeleted nor IncludeDeleted, the global HasQueryFilter
            // automatically applies !s.IsDeleted, so no additional filter needed
            return query;
        }

        public IQueryable<Domain.Entities.Store.Store> ApplySorting(IQueryable<Domain.Entities.Store.Store> query, StoreSearchCriteria searchCriteria)
        {
            return searchCriteria.SortBy?.ToLower() switch
            {
                "name" => searchCriteria.SortDescending ?
                    query.OrderByDescending(s => s.Name) :
                    query.OrderBy(s => s.Name),
                "createdat" => searchCriteria.SortDescending ?
                    query.OrderByDescending(s => s.CreatedAt) :
                    query.OrderBy(s => s.CreatedAt),
                _ => query.OrderBy(s => s.Name)
            };
        }
    }
}
