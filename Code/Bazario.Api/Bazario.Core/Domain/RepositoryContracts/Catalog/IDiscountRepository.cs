using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.Domain.RepositoryContracts.Catalog
{
    /// <summary>
    /// Repository contract for managing discount codes and promotions.
    /// </summary>
    public interface IDiscountRepository
    {
        /// <summary>
        /// Adds a new discount to the database.
        /// </summary>
        Task<Discount> AddDiscountAsync(Discount discount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing discount in the database.
        /// </summary>
        Task<Discount> UpdateDiscountAsync(Discount discount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a discount by setting IsActive = false.
        /// </summary>
        Task<bool> SoftDeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a discount by its ID.
        /// </summary>
        Task<Discount?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a discount by its code (case-insensitive).
        /// </summary>
        Task<Discount?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all active discounts for a specific store.
        /// </summary>
        Task<List<Discount>> GetDiscountsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all global discounts (not store-specific).
        /// </summary>
        Task<List<Discount>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a discount code can be used for a specific order.
        /// </summary>
        /// <param name="code">Discount code to validate</param>
        /// <param name="orderSubtotal">Order subtotal amount</param>
        /// <param name="storeIds">Store IDs in the order (for store-specific validation)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with discount details if valid</returns>
        Task<(bool IsValid, Discount? Discount, string? ErrorMessage)> ValidateDiscountAsync(
            string code, 
            decimal orderSubtotal, 
            List<Guid> storeIds, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a discount as used (one-time use only).
        /// </summary>
        Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts that are valid for a specific date range.
        /// </summary>
        Task<List<Discount>> GetValidDiscountsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts by type (Percentage or FixedAmount).
        /// </summary>
        Task<List<Discount>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for discounts to enable efficient filtering and pagination.
        /// </summary>
        IQueryable<Discount> GetDiscountsQueryable();

        /// <summary>
        /// Gets the count of discounts matching the query.
        /// </summary>
        Task<int> GetDiscountsCountAsync(IQueryable<Discount> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts with pagination from the query.
        /// </summary>
        Task<List<Discount>> GetDiscountsPagedAsync(IQueryable<Discount> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts that are about to expire within the specified days.
        /// </summary>
        Task<List<Discount>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets usage statistics for a discount.
        /// </summary>
        Task<(int TotalCreated, int TotalUsed, int TotalActive)> GetDiscountUsageStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple discounts by their codes in a single query to avoid N+1 problem.
        /// </summary>
        Task<List<Discount>> GetDiscountsByCodesAsync(List<string> codes, CancellationToken cancellationToken = default);
    }
}
