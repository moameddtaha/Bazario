using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Review
{
    /// <summary>
    /// Service for managing review CRUD operations
    /// </summary>
    public interface IReviewManagementService
    {
        Task<ReviewResponse> CreateReviewAsync(ReviewAddRequest reviewAddRequest, CancellationToken cancellationToken = default);
        Task<ReviewResponse> UpdateReviewAsync(ReviewUpdateRequest reviewUpdateRequest, CancellationToken cancellationToken = default);
        Task<bool> DeleteReviewAsync(Guid reviewId, Guid requestingUserId, CancellationToken cancellationToken = default);
        Task<ReviewResponse?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<PagedResponse<ReviewResponse>> GetReviewsByProductIdAsync(Guid productId, int pageNumber = 1, int pageSize = 10, ReviewSortBy sortBy = ReviewSortBy.Newest, CancellationToken cancellationToken = default);
        Task<List<ReviewResponse>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    }
}
