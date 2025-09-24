using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Store;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewRepository> _logger;

        public ReviewRepository(ApplicationDbContext context, ILogger<ReviewRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Review> AddReviewAsync(Review review, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new review for product: {ProductId}", review?.ProductId);
            
            try
            {
                // Validate input
                if (review == null)
                {
                    _logger.LogWarning("Attempted to add null review");
                    throw new ArgumentNullException(nameof(review));
                }

                _logger.LogDebug("Adding review to database context. ReviewId: {ReviewId}, ProductId: {ProductId}, CustomerId: {CustomerId}, Rating: {Rating}", 
                    review.ReviewId, review.ProductId, review.CustomerId, review.Rating);

                // Add review to context
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added review. ReviewId: {ReviewId}, ProductId: {ProductId}, Rating: {Rating}", 
                    review.ReviewId, review.ProductId, review.Rating);

                return review;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding review for product: {ProductId}", review?.ProductId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating review for product: {ProductId}", review?.ProductId);
                throw new InvalidOperationException($"Unexpected error while creating review: {ex.Message}", ex);
            }
        }

        public async Task<Review> UpdateReviewAsync(Review review, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update review: {ReviewId}", review?.ReviewId);
            
            try
            {
                // Validate input
                if (review == null)
                {
                    _logger.LogWarning("Attempted to update null review");
                    throw new ArgumentNullException(nameof(review));
                }

                if (review.ReviewId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update review with empty ID");
                    throw new ArgumentException("Review ID cannot be empty", nameof(review));
                }

                _logger.LogDebug("Checking if review exists in database. ReviewId: {ReviewId}", review.ReviewId);

                // Check if review exists (use FindAsync for simple PK lookup)
                var existingReview = await _context.Reviews.FindAsync(new object[] { review.ReviewId }, cancellationToken);
                if (existingReview == null)
                {
                    _logger.LogWarning("Review not found for update. ReviewId: {ReviewId}", review.ReviewId);
                    throw new InvalidOperationException($"Review with ID {review.ReviewId} not found");
                }

                _logger.LogDebug("Updating review properties. ReviewId: {ReviewId}, Rating: {Rating}", 
                    review.ReviewId, review.Rating);

                // Update only specific properties (not foreign keys or primary key) - only if provided
                if (review.Rating > 0 && review.Rating <= 5) // Only update if rating is provided and valid
                {
                    existingReview.Rating = review.Rating;
                }

                if (review.Comment != null) // Only update if comment is provided
                {
                    existingReview.Comment = review.Comment;
                }
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated review. ReviewId: {ReviewId}, Rating: {Rating}", 
                    review.ReviewId, review.Rating);

                return existingReview;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating review: {ReviewId}", review?.ReviewId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating review: {ReviewId}", review?.ReviewId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating review: {ReviewId}", review?.ReviewId);
                throw new InvalidOperationException($"Unexpected error while updating review with ID {review?.ReviewId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete review: {ReviewId}", reviewId);
            
            try
            {
                // Validate input
                if (reviewId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete review with empty ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Checking if review exists for deletion. ReviewId: {ReviewId}", reviewId);

                // Use FindAsync for simple PK lookup (no navigation properties needed for delete)
                var review = await _context.Reviews.FindAsync(new object[] { reviewId }, cancellationToken);
                if (review == null)
                {
                    _logger.LogWarning("Review not found for deletion. ReviewId: {ReviewId}", reviewId);
                    return false; // Review not found
                }

                _logger.LogDebug("Removing review from database context. ReviewId: {ReviewId}, ProductId: {ProductId}", 
                    reviewId, review.ProductId);

                // Delete the review
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted review. ReviewId: {ReviewId}, ProductId: {ProductId}", 
                    reviewId, review.ProductId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting review: {ReviewId}", reviewId);
                throw new InvalidOperationException($"Unexpected error while deleting review with ID {reviewId}: {ex.Message}", ex);
            }
        }

        public async Task<Review?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve review by ID: {ReviewId}", reviewId);
            
            try
            {
                // Validate input
                if (reviewId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve review with empty ID");
                    return null; // Invalid ID
                }

                _logger.LogDebug("Querying review with navigation properties. ReviewId: {ReviewId}", reviewId);

                // Find the review with navigation properties
                var review = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);

                if (review == null)
                {
                    _logger.LogDebug("Review not found. ReviewId: {ReviewId}", reviewId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved review. ReviewId: {ReviewId}, ProductId: {ProductId}, CustomerId: {CustomerId}, Rating: {Rating}", 
                        review.ReviewId, review.ProductId, review.CustomerId, review.Rating);
                }

                return review;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve review: {ReviewId}", reviewId);
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
                _logger.LogError(ex, "Failed to retrieve all reviews");
                throw new InvalidOperationException($"Failed to retrieve reviews: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve reviews for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reviews with empty product ID");
                    return new List<Review>(); // Invalid ID, return empty list
                }

                _logger.LogDebug("Querying reviews for product with navigation properties. ProductId: {ProductId}", productId);

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.ProductId == productId)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {ReviewCount} reviews for product: {ProductId}", reviews.Count, productId);

                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve reviews for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to retrieve reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve reviews for customer: {CustomerId}", customerId);
            
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve reviews with empty customer ID");
                    return new List<Review>(); // Invalid ID, return empty list
                }

                _logger.LogDebug("Querying reviews for customer with navigation properties. CustomerId: {CustomerId}", customerId);

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.CustomerId == customerId)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {ReviewCount} reviews for customer: {CustomerId}", reviews.Count, customerId);

                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve reviews for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to retrieve reviews for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve reviews by rating: {Rating}", rating);
            
            try
            {
                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    _logger.LogWarning("Attempted to retrieve reviews with invalid rating: {Rating}", rating);
                    return new List<Review>(); // Invalid rating, return empty list
                }

                _logger.LogDebug("Querying reviews by rating with navigation properties. Rating: {Rating}", rating);

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.Rating == rating)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {ReviewCount} reviews with rating: {Rating}", reviews.Count, rating);

                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve reviews with rating: {Rating}", rating);
                throw new InvalidOperationException($"Failed to retrieve reviews with rating {rating}: {ex.Message}", ex);
            }
        }

        public async Task<List<Review>> GetFilteredReviewsAsync(Expression<Func<Review, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered reviews");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve reviews with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Querying filtered reviews with navigation properties");

                var reviews = await _context.Reviews
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(predicate)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Successfully retrieved {ReviewCount} filtered reviews", reviews.Count);

                return reviews;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered reviews");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered reviews");
                throw new InvalidOperationException($"Failed to retrieve filtered reviews: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetAverageRatingByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to calculate average rating for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to calculate average rating with empty product ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Calculating average rating for product. ProductId: {ProductId}", productId);

                var averageRating = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .AverageAsync(r => (decimal)r.Rating, cancellationToken);

                _logger.LogDebug("Successfully calculated average rating for product {ProductId}: {AverageRating:F2}", productId, averageRating);

                return averageRating;
            }
            catch (InvalidOperationException)
            {
                _logger.LogDebug("No reviews found for product {ProductId}, returning 0", productId);
                // No reviews found, return 0
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate average rating for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to calculate average rating for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetReviewCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count reviews for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to count reviews with empty product ID");
                    return 0; // Invalid ID, return 0
                }

                _logger.LogDebug("Counting reviews for product. ProductId: {ProductId}", productId);

                var count = await _context.Reviews
                    .CountAsync(r => r.ProductId == productId, cancellationToken);

                _logger.LogDebug("Successfully counted reviews for product {ProductId}: {ReviewCount}", productId, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count reviews for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to count reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetReviewCountByRatingAsync(int rating, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to count reviews by rating: {Rating}", rating);
            
            try
            {
                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    _logger.LogWarning("Attempted to count reviews with invalid rating: {Rating}", rating);
                    return 0; // Invalid rating, return 0
                }

                _logger.LogDebug("Counting reviews by rating. Rating: {Rating}", rating);

                var count = await _context.Reviews
                    .CountAsync(r => r.Rating == rating, cancellationToken);

                _logger.LogDebug("Successfully counted reviews with rating {Rating}: {ReviewCount}", rating, count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count reviews with rating: {Rating}", rating);
                throw new InvalidOperationException($"Failed to count reviews with rating {rating}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete reviews for product: {ProductId}", productId);
            
            try
            {
                // Validate input
                if (productId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete reviews with empty product ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Finding reviews to delete for product. ProductId: {ProductId}", productId);

                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .ToListAsync(cancellationToken);

                if (!reviews.Any())
                {
                    _logger.LogDebug("No reviews found to delete for product {ProductId}", productId);
                    return true; // No reviews to delete, consider it successful
                }

                _logger.LogDebug("Removing {ReviewCount} reviews for product from database context. ProductId: {ProductId}", 
                    reviews.Count, productId);

                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted {ReviewCount} reviews for product. ProductId: {ProductId}", 
                    reviews.Count, productId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete reviews for product: {ProductId}", productId);
                throw new InvalidOperationException($"Failed to delete reviews for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReviewsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete reviews for customer: {CustomerId}", customerId);
            
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete reviews with empty customer ID");
                    return false; // Invalid ID
                }

                _logger.LogDebug("Finding reviews to delete for customer. CustomerId: {CustomerId}", customerId);

                var reviews = await _context.Reviews
                    .Where(r => r.CustomerId == customerId)
                    .ToListAsync(cancellationToken);

                if (!reviews.Any())
                {
                    _logger.LogDebug("No reviews found to delete for customer {CustomerId}", customerId);
                    return true; // No reviews to delete, consider it successful
                }

                _logger.LogDebug("Removing {ReviewCount} reviews for customer from database context. CustomerId: {CustomerId}", 
                    reviews.Count, customerId);

                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted {ReviewCount} reviews for customer. CustomerId: {CustomerId}", 
                    reviews.Count, customerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete reviews for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to delete reviews for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<StoreReviewStats> GetStoreReviewStatsAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting store review stats for store: {StoreId}", storeId);
            
            try
            {
                // Get aggregated review statistics for all products in this store
                var reviewStats = await _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.Product != null && r.Product.StoreId == storeId)
                    .GroupBy(r => 1) // Group all reviews together
                    .Select(g => new StoreReviewStats
                    {
                        TotalReviews = g.Count(),
                        AverageRating = g.Average(r => (decimal)r.Rating)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                // If no reviews found, return default values
                var result = reviewStats ?? new StoreReviewStats
                {
                    TotalReviews = 0,
                    AverageRating = 0
                };

                _logger.LogDebug("Successfully retrieved store review stats for store: {StoreId}. Reviews: {TotalReviews}, AvgRating: {AverageRating:F2}", 
                    storeId, result.TotalReviews, result.AverageRating);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get store review stats for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to get store review stats for store {storeId}: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<Guid, StoreReviewStats>> GetBulkStoreReviewStatsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting bulk store review stats for {StoreCount} stores", storeIds.Count);

            try
            {
                if (!storeIds.Any())
                {
                    return new Dictionary<Guid, StoreReviewStats>();
                }

                // Get all reviews for products in the specified stores in a single query
                var reviews = await _context.Reviews
                    .Where(r => r.Product != null && storeIds.Contains(r.Product.StoreId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Group by store and calculate stats
                var result = new Dictionary<Guid, StoreReviewStats>();

                foreach (var storeId in storeIds)
                {
                    var storeReviews = reviews.Where(r => r.Product?.StoreId == storeId).ToList();
                    
                    if (!storeReviews.Any())
                    {
                        result[storeId] = new StoreReviewStats
                        {
                            TotalReviews = 0,
                            AverageRating = 0
                        };
                        continue;
                    }

                    var totalReviews = storeReviews.Count;
                    var averageRating = storeReviews.Average(r => (decimal)r.Rating);

                    result[storeId] = new StoreReviewStats
                    {
                        TotalReviews = totalReviews,
                        AverageRating = averageRating
                    };
                }

                _logger.LogDebug("Successfully calculated bulk store review stats for {StoreCount} stores", storeIds.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bulk store review stats for {StoreCount} stores", storeIds.Count);
                throw new InvalidOperationException($"Failed to get bulk store review stats: {ex.Message}", ex);
            }
        }
    }
}
