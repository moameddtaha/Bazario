using System;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Aggregated review statistics for a store
    /// </summary>
    public class StoreReviewStats
    {
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }
    }
}
