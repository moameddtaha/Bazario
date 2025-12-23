using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Service for discount validation business rules.
    /// Returns DTOs instead of entities to maintain service layer abstraction.
    /// </summary>
    public class DiscountValidationService : IDiscountValidationService
    {
        private readonly IDiscountRepository _discountRepository;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly ILogger<DiscountValidationService> _logger;
        private readonly TimeProvider _timeProvider;

        public DiscountValidationService(
            IDiscountRepository discountRepository,
            IConcurrencyHelper concurrencyHelper,
            ILogger<DiscountValidationService> logger,
            TimeProvider? timeProvider = null)
        {
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<(bool IsValid, DiscountResponse? Discount, string? ErrorMessage)> ValidateDiscountCodeAsync(
            string code,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("ValidateDiscountCodeAsync called with null or empty code");
                throw new ArgumentException("Discount code cannot be null or empty", nameof(code));
            }

            if (storeIds == null || storeIds.Count == 0)
            {
                _logger.LogWarning("ValidateDiscountCodeAsync called with null or empty store IDs");
                throw new ArgumentException("Store IDs cannot be null or empty", nameof(storeIds));
            }

            if (orderSubtotal <= 0)
            {
                _logger.LogWarning("ValidateDiscountCodeAsync called with invalid subtotal: {Subtotal}", orderSubtotal);
                throw new ArgumentOutOfRangeException(nameof(orderSubtotal), "Order subtotal must be greater than 0");
            }

            // Normalize code for consistency (matches DiscountManagementService pattern)
            code = code.Trim().ToUpper();

            _logger.LogDebug("Validating discount code: {Code} for subtotal: {Subtotal}", code, orderSubtotal);

            var result = await _discountRepository.ValidateDiscountAsync(code, orderSubtotal, storeIds, cancellationToken);

            if (!result.IsValid)
            {
                _logger.LogWarning("Discount validation failed for code: {Code}. Reason: {ErrorMessage}", code, result.ErrorMessage);
                return (false, null, result.ErrorMessage);
            }

            // Assert that valid results have non-null discount (repository contract)
            if (result.Discount == null)
            {
                _logger.LogError("Repository returned IsValid=true but Discount=null for code: {Code}. This indicates a repository bug.", code);
                throw new InvalidOperationException($"Invalid repository state: discount '{code}' marked as valid but entity is null");
            }

            _logger.LogDebug("Discount code validated successfully: {Code}", code);

            // Discount guaranteed non-null here
            var discountResponse = DiscountResponse.FromDiscount(result.Discount);
            return (true, discountResponse, null);
        }

        public async Task<(List<DiscountResponse> ValidDiscounts, List<string> ErrorMessages)> ValidateMultipleDiscountCodesAsync(
            List<string> codes,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default)
        {
            if (codes == null || codes.Count == 0)
            {
                _logger.LogWarning("ValidateMultipleDiscountCodesAsync called with null or empty codes");
                throw new ArgumentException("Discount codes cannot be null or empty", nameof(codes));
            }

            if (storeIds == null || storeIds.Count == 0)
            {
                _logger.LogWarning("ValidateMultipleDiscountCodesAsync called with null or empty store IDs");
                throw new ArgumentException("Store IDs cannot be null or empty", nameof(storeIds));
            }

            if (orderSubtotal <= 0)
            {
                _logger.LogWarning("ValidateMultipleDiscountCodesAsync called with invalid subtotal: {Subtotal}", orderSubtotal);
                throw new ArgumentOutOfRangeException(nameof(orderSubtotal), "Order subtotal must be greater than 0");
            }

            _logger.LogDebug("Validating {Count} discount codes for subtotal: {Subtotal}", codes.Count, orderSubtotal);

            var validDiscounts = new List<DiscountResponse>();
            var errorMessages = new List<string>();

            foreach (var code in codes)
            {
                try
                {
                    var (isValid, discount, errorMessage) = await ValidateDiscountCodeAsync(code, orderSubtotal, storeIds, cancellationToken);

                    if (isValid && discount != null)
                    {
                        validDiscounts.Add(discount);
                        _logger.LogDebug("Discount code validated successfully: {Code}", code);
                    }
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessages.Add($"{code}: {errorMessage}");
                        _logger.LogWarning("Discount code validation failed: {Code}. Reason: {ErrorMessage}", code, errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{code}: Validation error: {ex.Message}";
                    errorMessages.Add(errorMsg);
                    _logger.LogError(ex, "Unexpected error validating discount code: {Code}", code);
                }
            }

            _logger.LogInformation(
                "Batch validation complete: {ValidCount} valid, {FailedCount} invalid out of {TotalCount} codes",
                validDiscounts.Count,
                errorMessages.Count,
                codes.Count);

            if (errorMessages.Count > 0)
            {
                _logger.LogWarning("Failed discount codes: {@FailedCodes}", errorMessages.Take(10));
            }

            return (validDiscounts, errorMessages);
        }

        public async Task<bool> DiscountExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("DiscountExistsAsync called with null or empty code");
                throw new ArgumentException("Discount code cannot be null or empty", nameof(code));
            }

            // Normalize code for consistency
            code = code.Trim().ToUpper();

            var discount = await _discountRepository.GetDiscountByCodeAsync(code, cancellationToken);
            return discount != null;
        }

        public async Task<bool> IsDiscountCodeUniqueAsync(string code, Guid? excludeDiscountId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("IsDiscountCodeUniqueAsync called with null or empty code");
                throw new ArgumentException("Discount code cannot be null or empty", nameof(code));
            }

            // Normalize code for consistency
            code = code.Trim().ToUpper();

            var existingDiscount = await _discountRepository.GetDiscountByCodeAsync(code, cancellationToken);

            if (existingDiscount == null)
            {
                return true;
            }

            // If updating, check if the existing discount is the same one being updated
            if (excludeDiscountId.HasValue && existingDiscount.DiscountId == excludeDiscountId.Value)
            {
                return true;
            }

            return false;
        }

        public bool ValidateDiscountValue(DiscountType type, decimal value)
        {
            return type switch
            {
                DiscountType.Percentage => value > 0 && value <= 100, // Percentage should be 1-100 (1%-100%)
                DiscountType.FixedAmount => value > 0, // Fixed amount should be positive
                _ => false
            };
        }

        public bool ValidateDateRange(DateTime validFrom, DateTime validTo)
        {
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            // ValidFrom should be now or in future for new discounts
            if (validFrom < utcNow)
            {
                return false;
            }

            // ValidTo must be after ValidFrom
            if (validTo <= validFrom)
            {
                return false;
            }

            // Enforce reasonable validity window (max 2 years)
            if ((validTo - validFrom).TotalDays > 730)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            if (discountId == Guid.Empty)
            {
                _logger.LogWarning("MarkDiscountAsUsedAsync called with empty discount ID");
                throw new ArgumentException("Discount ID cannot be empty", nameof(discountId));
            }

            _logger.LogInformation("Marking discount as used: {DiscountId}", discountId);

            // Execute with retry logic for optimistic concurrency - critical for preventing double-use
            var result = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
            {
                return await _discountRepository.MarkDiscountAsUsedAsync(discountId, cancellationToken);
            }, "MarkDiscountAsUsed", cancellationToken);

            if (result)
            {
                _logger.LogInformation("Discount marked as used successfully: {DiscountId}", discountId);
            }
            else
            {
                _logger.LogWarning("Failed to mark discount as used: {DiscountId}", discountId);
            }

            return result;
        }
    }
}
