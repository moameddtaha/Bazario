using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Service for managing discount CRUD operations.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// </summary>
    public class DiscountManagementService : IDiscountManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly ILogger<DiscountManagementService> _logger;

        public DiscountManagementService(
            IUnitOfWork unitOfWork,
            IConcurrencyHelper concurrencyHelper,
            ILogger<DiscountManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Domain.Entities.Catalog.Discount> CreateDiscountAsync(
            string code,
            DiscountType type,
            decimal value,
            DateTime validFrom,
            DateTime validTo,
            decimal minimumOrderAmount,
            Guid? applicableStoreId,
            string? description,
            Guid createdBy,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating discount with code: {Code}", code);

            var discount = new Domain.Entities.Catalog.Discount
            {
                DiscountId = Guid.NewGuid(),
                Code = code.Trim().ToUpper(),
                Type = type,
                Value = value,
                ValidFrom = validFrom,
                ValidTo = validTo,
                MinimumOrderAmount = minimumOrderAmount,
                ApplicableStoreId = applicableStoreId,
                Description = description,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsUsed = false
            };

            var createdDiscount = await _unitOfWork.Discounts.AddDiscountAsync(discount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Discount created successfully with ID: {DiscountId}", createdDiscount.DiscountId);

            return createdDiscount;
        }

        public async Task<Domain.Entities.Catalog.Discount> UpdateDiscountAsync(
            Guid discountId,
            string? code,
            DiscountType? type,
            decimal? value,
            DateTime? validFrom,
            DateTime? validTo,
            decimal? minimumOrderAmount,
            string? description,
            Guid updatedBy,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating discount with ID: {DiscountId}", discountId);

            // Execute with retry logic for optimistic concurrency
            var updatedDiscount = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
            {
                // Fetch fresh entity on each retry attempt
                var existingDiscount = await _unitOfWork.Discounts.GetDiscountByIdAsync(discountId, cancellationToken);

                if (existingDiscount == null)
                {
                    _logger.LogWarning("Discount not found with ID: {DiscountId}", discountId);
                    throw new InvalidOperationException($"Discount with ID {discountId} not found");
                }

                // Safe Update Pattern: Only update provided fields
                if (code != null) existingDiscount.Code = code.Trim().ToUpper();
                if (type.HasValue) existingDiscount.Type = type.Value;
                if (value.HasValue) existingDiscount.Value = value.Value;
                if (validFrom.HasValue) existingDiscount.ValidFrom = validFrom.Value;
                if (validTo.HasValue) existingDiscount.ValidTo = validTo.Value;
                if (minimumOrderAmount.HasValue) existingDiscount.MinimumOrderAmount = minimumOrderAmount.Value;
                if (description != null) existingDiscount.Description = description;

                existingDiscount.UpdatedBy = updatedBy;
                existingDiscount.UpdatedAt = DateTime.UtcNow;

                var result = await _unitOfWork.Discounts.UpdateDiscountAsync(existingDiscount, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return result;
            }, "UpdateDiscount", cancellationToken);

            _logger.LogInformation("Discount updated successfully with ID: {DiscountId}", discountId);

            return updatedDiscount;
        }

        public async Task<bool> DeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting discount with ID: {DiscountId}", discountId);

            var result = await _unitOfWork.Discounts.SoftDeleteDiscountAsync(discountId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (result)
            {
                _logger.LogInformation("Discount deleted successfully with ID: {DiscountId}", discountId);
            }
            else
            {
                _logger.LogWarning("Discount not found for deletion with ID: {DiscountId}", discountId);
            }

            return result;
        }

        public async Task<Domain.Entities.Catalog.Discount?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount by ID: {DiscountId}", discountId);
            return await _unitOfWork.Discounts.GetDiscountByIdAsync(discountId, cancellationToken);
        }

        public async Task<Domain.Entities.Catalog.Discount?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount by code: {Code}", code);
            return await _unitOfWork.Discounts.GetDiscountByCodeAsync(code, cancellationToken);
        }

        public async Task<List<Domain.Entities.Catalog.Discount>> GetStoreDiscountsAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discounts for store ID: {StoreId}", storeId);
            return await _unitOfWork.Discounts.GetDiscountsByStoreIdAsync(storeId, cancellationToken);
        }

        public async Task<List<Domain.Entities.Catalog.Discount>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting global discounts");
            return await _unitOfWork.Discounts.GetGlobalDiscountsAsync(cancellationToken);
        }

        public async Task<List<Domain.Entities.Catalog.Discount>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discounts by type: {Type}", type);
            return await _unitOfWork.Discounts.GetDiscountsByTypeAsync(type, cancellationToken);
        }

        public async Task<List<Domain.Entities.Catalog.Discount>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discounts expiring within {Days} days", daysUntilExpiry);
            return await _unitOfWork.Discounts.GetExpiringDiscountsAsync(daysUntilExpiry, cancellationToken);
        }

        public async Task<List<Domain.Entities.Catalog.Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting active discounts");

            var query = _unitOfWork.Discounts.GetDiscountsQueryable()
                .Where(d => d.IsActive && !d.IsUsed && d.ValidTo >= DateTime.UtcNow);

            return await _unitOfWork.Discounts.GetDiscountsPagedAsync(query, 1, int.MaxValue, cancellationToken);
        }
    }
}
