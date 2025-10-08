using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReviewEntity = Bazario.Core.Domain.Entities.Review.Review;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Review;

namespace Bazario.Core.Extensions.Review
{
    public static class ReviewExtensions
    {
        public static ReviewResponse ToReviewResponse(this ReviewEntity review)
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
