using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Review;
using Bazario.Core.ServiceContracts.Review;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Review
{
    /// <summary>
    /// Service for validating review eligibility and business rules
    /// </summary>
    public class ReviewValidationService : IReviewValidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewValidationService> _logger;

        public ReviewValidationService(
            IUnitOfWork unitOfWork,
            ILogger<ReviewValidationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates if a customer can review a specific product
        /// </summary>
        public async Task<ReviewValidationResult> ValidateReviewEligibilityAsync(
            Guid customerId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (customerId == Guid.Empty)
            {
                _logger.LogWarning("ValidateReviewEligibilityAsync called with empty customer ID");
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
            }

            if (productId == Guid.Empty)
            {
                _logger.LogWarning("ValidateReviewEligibilityAsync called with empty product ID");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            _logger.LogDebug("Validating review eligibility for customer {CustomerId} and product {ProductId}", customerId, productId);

            var result = new ReviewValidationResult
            {
                ValidationMessages = new System.Collections.Generic.List<string>()
            };

            // Check if customer has purchased the product
            var hasPurchased = await _unitOfWork.Orders.HasCustomerPurchasedProductAsync(customerId, productId, cancellationToken);
            result.HasPurchased = hasPurchased;

            if (!hasPurchased)
            {
                result.CanReview = false;
                result.ValidationMessages.Add("You must purchase this product before reviewing it");
                _logger.LogDebug("Customer {CustomerId} has not purchased product {ProductId}", customerId, productId);
                return result;
            }

            // Get purchase date
            result.PurchaseDate = await _unitOfWork.Orders.GetProductPurchaseDateAsync(customerId, productId, cancellationToken);

            // Check if customer has already reviewed the product
            var alreadyReviewed = await HasCustomerReviewedProductAsync(customerId, productId, cancellationToken);
            result.AlreadyReviewed = alreadyReviewed;

            if (alreadyReviewed)
            {
                result.CanReview = false;
                result.ValidationMessages.Add("You have already reviewed this product");
                _logger.LogDebug("Customer {CustomerId} has already reviewed product {ProductId}", customerId, productId);
                return result;
            }

            // All validations passed
            result.CanReview = true;
            _logger.LogDebug("Customer {CustomerId} is eligible to review product {ProductId}", customerId, productId);

            return result;
        }

        /// <summary>
        /// Validates if a customer has already reviewed a product
        /// </summary>
        public async Task<bool> HasCustomerReviewedProductAsync(
            Guid customerId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (customerId == Guid.Empty)
            {
                _logger.LogWarning("HasCustomerReviewedProductAsync called with empty customer ID");
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
            }

            if (productId == Guid.Empty)
            {
                _logger.LogWarning("HasCustomerReviewedProductAsync called with empty product ID");
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            var reviews = await _unitOfWork.Reviews.GetReviewsByCustomerIdAsync(customerId, cancellationToken);
            return reviews.Any(r => r.ProductId == productId);
        }

        /// <summary>
        /// Validates rating value (must be between 1 and 5)
        /// </summary>
        public bool ValidateRating(int rating)
        {
            return rating >= 1 && rating <= 5;
        }
    }
}
