using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Review;
using Bazario.Core.Models.Review;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Review
{
    /// <summary>
    /// Service for review moderation operations
    /// </summary>
    public interface IReviewModerationService
    {
        Task<ReviewModerationResult> ModerateReviewAsync(Guid reviewId, ModerationAction moderationAction, Guid moderatorId, string? reason = null, CancellationToken cancellationToken = default);
        Task<PagedResponse<ReviewResponse>> GetReviewsPendingModerationAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        Task<bool> ReportReviewAsync(Guid reviewId, Guid reporterId, string reason, CancellationToken cancellationToken = default);
    }
}
