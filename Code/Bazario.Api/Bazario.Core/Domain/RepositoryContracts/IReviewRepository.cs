using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Models.Store;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface IReviewRepository
    {
        Task<Review> AddReviewAsync(Review review, CancellationToken cancellationToken = default);

        Task<Review> UpdateReviewAsync(Review review, CancellationToken cancellationToken = default);

        Task<bool> DeleteReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

        Task<Review?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

        Task<List<Review>> GetAllReviewsAsync(CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default);

        Task<List<Review>> GetFilteredReviewsAsync(Expression<Func<Review, bool>> predicate, CancellationToken cancellationToken = default);

        Task<decimal> GetAverageRatingByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<int> GetReviewCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<int> GetReviewCountByRatingAsync(int rating, CancellationToken cancellationToken = default);

        Task<bool> DeleteReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<bool> DeleteReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated review statistics for products in a specific store
        /// </summary>
        Task<StoreReviewStats> GetStoreReviewStatsAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets store review statistics for multiple stores in a single query
        /// </summary>
        Task<Dictionary<Guid, StoreReviewStats>> GetBulkStoreReviewStatsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default);
    }
}
