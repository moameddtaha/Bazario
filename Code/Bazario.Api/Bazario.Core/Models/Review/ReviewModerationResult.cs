using System;

namespace Bazario.Core.Models.Review
{
    /// <summary>
    /// Review moderation result
    /// </summary>
    public class ReviewModerationResult
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public ModerationAction ActionTaken { get; set; }
        public DateTime ActionDate { get; set; }
        public Guid ModeratorId { get; set; }
    }
}
