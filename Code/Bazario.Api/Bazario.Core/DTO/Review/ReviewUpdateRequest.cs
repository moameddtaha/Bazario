using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReviewEntity = Bazario.Core.Domain.Entities.Review.Review;

namespace Bazario.Core.DTO.Review
{
    public class ReviewUpdateRequest
    {
        [Required(ErrorMessage = "Review Id cannot be blank")]
        public Guid ReviewId { get; set; }

        [Display(Name = "Rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int? Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        public byte[]? RowVersion { get; set; }

        public ReviewEntity ToReview()
        {
            return new ReviewEntity
            {
                ReviewId = ReviewId,
                Rating = Rating ?? 0, // Use 0 to indicate not provided
                Comment = Comment
            };
        }
    }
}
