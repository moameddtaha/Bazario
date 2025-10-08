using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Review;
using Bazario.Core.Models.Store;
using ReviewEntity = Bazario.Core.Domain.Entities.Review.Review;

namespace Bazario.Core.Domain.RepositoryContracts.Review
{
    public interface IReviewRepository
    {
        Task<ReviewEntity> AddReviewAsync(ReviewEntity review, CancellationToken cancellationToken = default);

        Task<ReviewEntity> UpdateReviewAsync(ReviewEntity review, CancellationToken cancellationToken = default);

        Task<bool> DeleteReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

        Task<ReviewEntity?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

        Task<List<ReviewEntity>> GetAllReviewsAsync(CancellationToken cancellationToken = default);

        Task<List<ReviewEntity>> GetReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<List<ReviewEntity>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<List<ReviewEntity>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default);

        Task<List<ReviewEntity>> GetFilteredReviewsAsync(Expression<Func<ReviewEntity, bool>> predicate, CancellationToken cancellationToken = default);

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
