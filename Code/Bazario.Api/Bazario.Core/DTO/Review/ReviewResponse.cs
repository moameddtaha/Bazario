using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Core.DTO.Review
{
    public class ReviewResponse
    {
        public Guid ReviewId { get; set; }

        [Display(Name = "Customer Id")]
        public Guid CustomerId { get; set; }

        [Display(Name = "Product Id")]
        public Guid ProductId { get; set; }

        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Star Rating")]
        public string StarRating => new string('★', Rating) + new string('☆', 5 - Rating);

        public override bool Equals(object? obj)
        {
            return obj is ReviewResponse response &&
                   ReviewId.Equals(response.ReviewId) &&
                   CustomerId.Equals(response.CustomerId) &&
                   ProductId.Equals(response.ProductId) &&
                   Rating == response.Rating &&
                   Comment == response.Comment &&
                   CreatedAt == response.CreatedAt;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(ReviewId);
            hash.Add(CustomerId);
            hash.Add(ProductId);
            hash.Add(Rating);
            hash.Add(Comment);
            hash.Add(CreatedAt);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Review: ID: {ReviewId}, Rating: {Rating}/5, Product ID: {ProductId}, Customer ID: {CustomerId}";
        }

        public ReviewUpdateRequest ToReviewUpdateRequest()
        {
            return new ReviewUpdateRequest()
            {
                ReviewId = ReviewId,
                Rating = Rating,
                Comment = Comment
            };
        }
    }
}
