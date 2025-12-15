using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Service for discount validation business rules.
    /// </summary>
    public class DiscountValidationService : IDiscountValidationService
    {
        private readonly IDiscountRepository _discountRepository;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly ILogger<DiscountValidationService> _logger;

        public DiscountValidationService(
            IDiscountRepository discountRepository,
            IConcurrencyHelper concurrencyHelper,
            ILogger<DiscountValidationService> logger)
        {
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool IsValid, Domain.Entities.Catalog.Discount? Discount, string? ErrorMessage)> ValidateDiscountCodeAsync(
            string code,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating discount code: {Code} for subtotal: {Subtotal}", code, orderSubtotal);

            var result = await _discountRepository.ValidateDiscountAsync(code, orderSubtotal, storeIds, cancellationToken);

            if (!result.IsValid)
            {
                _logger.LogWarning("Discount validation failed for code: {Code}. Reason: {ErrorMessage}", code, result.ErrorMessage);
            }
            else
            {
                _logger.LogDebug("Discount code validated successfully: {Code}", code);
            }

            return result;
        }

        public async Task<(List<Domain.Entities.Catalog.Discount> ValidDiscounts, List<string> ErrorMessages)> ValidateMultipleDiscountCodesAsync(
            List<string> codes,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating {Count} discount codes for subtotal: {Subtotal}", codes.Count, orderSubtotal);

            var validDiscounts = new List<Domain.Entities.Catalog.Discount>();
            var errorMessages = new List<string>();

            foreach (var code in codes)
            {
                var (isValid, discount, errorMessage) = await ValidateDiscountCodeAsync(code, orderSubtotal, storeIds, cancellationToken);

                if (isValid && discount != null)
                {
                    validDiscounts.Add(discount);
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorMessages.Add($"{code}: {errorMessage}");
                }
            }

            _logger.LogInformation("{ValidCount} valid discounts out of {TotalCount} codes", validDiscounts.Count, codes.Count);

            return (validDiscounts, errorMessages);
        }

        public async Task<bool> DiscountExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            var discount = await _discountRepository.GetDiscountByCodeAsync(code, cancellationToken);
            return discount != null;
        }

        public async Task<bool> IsDiscountCodeUniqueAsync(string code, Guid? excludeDiscountId = null, CancellationToken cancellationToken = default)
        {
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
                DiscountType.Percentage => value > 0 && value <= 1, // Percentage should be 0-1 (0%-100%)
                DiscountType.FixedAmount => value > 0, // Fixed amount should be positive
                _ => false
            };
        }

        public bool ValidateDateRange(DateTime validFrom, DateTime validTo)
        {
            return validTo > validFrom && validFrom >= DateTime.UtcNow.AddDays(-1);
        }

        public async Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
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
