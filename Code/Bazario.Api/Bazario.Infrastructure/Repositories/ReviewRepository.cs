using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Review> AddReviewAsync(Review review, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (review == null)
                    throw new ArgumentNullException(nameof(review));

                // Add review to context
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync(cancellationToken);

                return review;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating review: {ex.Message}", ex);
            }
        }

        public async Task<Review> UpdateReviewAsync(Review review, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (review == null)
                    throw new ArgumentNullException(nameof(review));

                if (review.ReviewId == Guid.Empty)
                    throw new ArgumentException("Review ID cannot be empty", nameof(review));

                // Check if review exists (use FindAsync for simple PK lookup)
                var existingReview = await _context.Reviews.FindAsync(new object[] { review.ReviewId }, cancellationToken);
                if (existingReview == null)
                {
                    throw new InvalidOperationException($"Review with ID {review.ReviewId} not found");
                }

                // Update only specific properties (not foreign keys or primary key)
                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;
                
                await _context.SaveChangesAsync(cancellationToken);

                return existingReview;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while updating review with ID {review?.ReviewId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (reviewId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var review = await _context.Reviews.FindAsync(new object[] { reviewId }, cancellationToken);
                if (review == null)
                {
                    return false; // Review not found
                }

                // Delete the review
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting review with ID {reviewId}: {ex.Message}", ex);
            }
        }

        public async Task<Review?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (reviewId == Guid.Empty)
                {
                    return null; // Invalid ID
                }

                // Find the review with navigation properties
                var review = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);

                return review;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve review with ID {reviewId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetAllReviewsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .ToListAsync(cancellationToken);

                return reviews;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve reviews: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return new List<Review>(); // Invalid ID, return empty list
                }

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.ProductId == productId)
                    .ToListAsync(cancellationToken);

                return reviews;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    return new List<Review>(); // Invalid ID, return empty list
                }

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.CustomerId == customerId)
                    .ToListAsync(cancellationToken);

                return reviews;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve reviews for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return new List<Review>(); // Invalid rating, return empty list
                }

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.Rating == rating)
                    .ToListAsync(cancellationToken);

                return reviews;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve reviews with rating {rating}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetFilteredReviewsAsync(Expression<Func<Review, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                return reviews;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered reviews: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetAverageRatingByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var averageRating = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .AverageAsync(r => (decimal)r.Rating, cancellationToken);

                return averageRating;
            }
            catch (InvalidOperationException)
            {
                // No reviews found, return 0
                return 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate average rating for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetReviewCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return 0; // Invalid ID, return 0
                }

                var count = await _context.Reviews
                    .CountAsync(r => r.ProductId == productId, cancellationToken);

                return count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetReviewCountByRatingAsync(int rating, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return 0; // Invalid rating, return 0
                }

                var count = await _context.Reviews
                    .CountAsync(r => r.Rating == rating, cancellationToken);

                return count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count reviews with rating {rating}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .ToListAsync(cancellationToken);

                if (!reviews.Any())
                {
                    return true; // No reviews to delete, consider it successful
                }

                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    return false; // Invalid ID
                }

                var reviews = await _context.Reviews
                    .Where(r => r.CustomerId == customerId)
                    .ToListAsync(cancellationToken);

                if (!reviews.Any())
                {
                    return true; // No reviews to delete, consider it successful
                }

                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete reviews for customer {customerId}: {ex.Message}", ex);
            }
        }
    }
}
