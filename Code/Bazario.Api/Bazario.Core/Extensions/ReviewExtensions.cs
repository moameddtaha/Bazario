using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.DTO;

namespace Bazario.Core.Extensions
{
    public static class ReviewExtensions
    {
        public static ReviewResponse ToReviewResponse(this Review review)
        {
            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                CustomerId = review.CustomerId,
                ProductId = review.ProductId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
