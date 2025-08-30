using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;

namespace Bazario.Core.DTO
{
    public class ReviewUpdateRequest
    {
        [Required(ErrorMessage = "Review Id cannot be blank")]
        public Guid ReviewId { get; set; }

        [Required(ErrorMessage = "Rating cannot be blank")]
        [Display(Name = "Rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        public Review ToReview()
        {
            return new Review
            {
                ReviewId = ReviewId,
                Rating = Rating,
                Comment = Comment
            };
        }
    }
}
