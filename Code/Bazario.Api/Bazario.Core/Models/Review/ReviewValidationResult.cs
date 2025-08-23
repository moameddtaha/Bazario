using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Review
{
    /// <summary>
    /// Review validation result
    /// </summary>
    public class ReviewValidationResult
    {
        public bool CanReview { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
        public bool HasPurchased { get; set; }
        public bool AlreadyReviewed { get; set; }
        public DateTime? PurchaseDate { get; set; }
    }
}
