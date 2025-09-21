using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using ReviewEntity = Bazario.Core.Domain.Entities.Review;

namespace Bazario.Core.DTO.Review
{
    public class ReviewAddRequest
    {
        [Required(ErrorMessage = "Customer Id cannot be blank")]
        [Display(Name = "Customer Id")]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "Product Id cannot be blank")]
        [Display(Name = "Product Id")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Rating cannot be blank")]
        [Display(Name = "Rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        public ReviewEntity ToReview()
        {
            return new ReviewEntity
            {
                CustomerId = CustomerId,
                ProductId = ProductId,
                Rating = Rating,
                Comment = Comment,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
