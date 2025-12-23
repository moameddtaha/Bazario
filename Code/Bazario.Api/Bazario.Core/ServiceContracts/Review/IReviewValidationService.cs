using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Review;

namespace Bazario.Core.ServiceContracts.Review
{
    /// <summary>
    /// Service for validating review eligibility and business rules
    /// </summary>
    public interface IReviewValidationService
    {
        /// <summary>
        /// Validates if a customer can review a specific product
        /// </summary>
        Task<ReviewValidationResult> ValidateReviewEligibilityAsync(
            Guid customerId,
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a customer has already reviewed a product
        /// </summary>
        Task<bool> HasCustomerReviewedProductAsync(
            Guid customerId,
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates rating value
        /// </summary>
        bool ValidateRating(int rating);
    }
}
