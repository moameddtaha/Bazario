using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.Enums.Catalog;
using Bazario.Core.Exceptions.Catalog;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Catalog.Discount
{
    /// <summary>
    /// Service for managing discount CRUD operations.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// Uses DTOs for request/response to provide validation, security, and versioning.
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

        public async Task<DiscountResponse> CreateDiscountAsync(
            DiscountAddRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Creating discount with code: {Code}", request.Code);

            // Convert DTO to entity - uses ToDiscount() method
            var discount = request.ToDiscount();
            discount.DiscountId = Guid.NewGuid();
            // Normalize code: trim and uppercase for consistency
            discount.Code = discount.Code.Trim().ToUpper();

            var createdDiscount = await _unitOfWork.Discounts.AddDiscountAsync(discount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Discount created successfully with ID: {DiscountId}", createdDiscount.DiscountId);

            // Convert entity to response DTO
            return DiscountResponse.FromDiscount(createdDiscount);
        }

        public async Task<DiscountResponse> UpdateDiscountAsync(
            DiscountUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Updating discount with ID: {DiscountId}", request.DiscountId);

            // Execute with retry logic for optimistic concurrency
            var updatedDiscount = await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
            {
                // Fetch fresh entity on each retry attempt
                var existingDiscount = await _unitOfWork.Discounts.GetDiscountByIdAsync(request.DiscountId, cancellationToken);

                if (existingDiscount == null)
                {
                    _logger.LogWarning("Discount not found with ID: {DiscountId}", request.DiscountId);
                    throw new DiscountNotFoundException(request.DiscountId);
                }

                // Safe Update Pattern: Only update provided fields (non-null/non-sentinel values)
                if (request.Code != null && request.Code != string.Empty)
                    existingDiscount.Code = request.Code.Trim().ToUpper();
                if (request.Type.HasValue)
                    existingDiscount.Type = request.Type.Value;
                if (request.Value.HasValue && request.Value.Value > 0)
                    existingDiscount.Value = request.Value.Value;
                if (request.ValidFrom.HasValue && request.ValidFrom.Value != DateTime.MinValue)
                    existingDiscount.ValidFrom = request.ValidFrom.Value;
                if (request.ValidTo.HasValue && request.ValidTo.Value != DateTime.MinValue)
                    existingDiscount.ValidTo = request.ValidTo.Value;
                if (request.MinimumOrderAmount.HasValue && request.MinimumOrderAmount.Value >= 0)
                    existingDiscount.MinimumOrderAmount = request.MinimumOrderAmount.Value;
                if (request.Description != null)
                    existingDiscount.Description = request.Description;
                if (request.IsActive.HasValue)
                    existingDiscount.IsActive = request.IsActive.Value;

                existingDiscount.UpdatedBy = request.UpdatedBy;
                existingDiscount.UpdatedAt = DateTime.UtcNow;
                existingDiscount.RowVersion = request.RowVersion; // For optimistic concurrency

                var result = await _unitOfWork.Discounts.UpdateDiscountAsync(existingDiscount, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return result;
            }, "UpdateDiscount", cancellationToken);

            _logger.LogInformation("Discount updated successfully with ID: {DiscountId}", request.DiscountId);

            // Convert entity to response DTO
            return DiscountResponse.FromDiscount(updatedDiscount);
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

        public async Task<DiscountResponse?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discount by ID: {DiscountId}", discountId);
            var discount = await _unitOfWork.Discounts.GetDiscountByIdAsync(discountId, cancellationToken);
            return discount != null ? DiscountResponse.FromDiscount(discount) : null;
        }

        public async Task<DiscountResponse?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("GetDiscountByCodeAsync called with null or empty code");
                throw new ArgumentException("Discount code cannot be null or empty", nameof(code));
            }

            _logger.LogDebug("Getting discount by code: {Code}", code);
            var discount = await _unitOfWork.Discounts.GetDiscountByCodeAsync(code, cancellationToken);
            return discount != null ? DiscountResponse.FromDiscount(discount) : null;
        }

        public async Task<List<DiscountResponse>> GetStoreDiscountsAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                _logger.LogWarning("GetStoreDiscountsAsync called with empty store ID");
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            }

            _logger.LogDebug("Getting discounts for store ID: {StoreId}", storeId);
            var discounts = await _unitOfWork.Discounts.GetDiscountsByStoreIdAsync(storeId, cancellationToken);
            return discounts.Select(DiscountResponse.FromDiscount).ToList();
        }

        public async Task<List<DiscountResponse>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting global discounts");
            var discounts = await _unitOfWork.Discounts.GetGlobalDiscountsAsync(cancellationToken);
            return discounts.Select(DiscountResponse.FromDiscount).ToList();
        }

        public async Task<List<DiscountResponse>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting discounts by type: {Type}", type);
            var discounts = await _unitOfWork.Discounts.GetDiscountsByTypeAsync(type, cancellationToken);
            return discounts.Select(DiscountResponse.FromDiscount).ToList();
        }

        public async Task<List<DiscountResponse>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
        {
            if (daysUntilExpiry < 0)
            {
                _logger.LogWarning("GetExpiringDiscountsAsync called with negative days: {Days}", daysUntilExpiry);
                throw new ArgumentOutOfRangeException(nameof(daysUntilExpiry), "Days until expiry cannot be negative");
            }

            if (daysUntilExpiry > 365)
            {
                _logger.LogWarning("GetExpiringDiscountsAsync called with excessive days: {Days}", daysUntilExpiry);
                throw new ArgumentOutOfRangeException(nameof(daysUntilExpiry), "Days until expiry cannot exceed 365 days");
            }

            _logger.LogDebug("Getting discounts expiring within {Days} days", daysUntilExpiry);
            var discounts = await _unitOfWork.Discounts.GetExpiringDiscountsAsync(daysUntilExpiry, cancellationToken);
            return discounts.Select(DiscountResponse.FromDiscount).ToList();
        }

        public async Task<List<DiscountResponse>> GetActiveDiscountsAsync(
            int pageNumber = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("GetActiveDiscountsAsync called with invalid page number: {PageNumber}", pageNumber);
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1");
            }

            if (pageSize < 1 || pageSize > 1000)
            {
                _logger.LogWarning("GetActiveDiscountsAsync called with invalid page size: {PageSize}", pageSize);
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 1000");
            }

            _logger.LogDebug("Getting active discounts (page {Page}, size {Size})", pageNumber, pageSize);

            var query = _unitOfWork.Discounts.GetDiscountsQueryable()
                .Where(d => d.IsActive && !d.IsUsed && d.ValidTo >= DateTime.UtcNow);

            var discounts = await _unitOfWork.Discounts.GetDiscountsPagedAsync(query, pageNumber, pageSize, cancellationToken);
            return discounts.Select(DiscountResponse.FromDiscount).ToList();
        }
    }
}
