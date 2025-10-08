using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Catalog;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.ServiceContracts.Catalog.Discount
{
    /// <summary>
    /// Service contract for managing discount CRUD operations.
    /// Handles creation, updating, retrieval, and deletion of discount codes.
    /// </summary>
    public interface IDiscountManagementService
    {
        /// <summary>
        /// Creates a new discount code.
        /// </summary>
        /// <param name="code">Unique discount code</param>
        /// <param name="type">Type of discount (Percentage or FixedAmount)</param>
        /// <param name="value">Discount value</param>
        /// <param name="validFrom">Start date of validity</param>
        /// <param name="validTo">End date of validity</param>
        /// <param name="minimumOrderAmount">Minimum order amount required</param>
        /// <param name="applicableStoreId">Store ID for store-specific discounts (null for global)</param>
        /// <param name="description">Optional description</param>
        /// <param name="createdBy">User ID who created the discount</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created discount entity</returns>
        Task<Domain.Entities.Catalog.Discount> CreateDiscountAsync(
            string code,
            DiscountType type,
            decimal value,
            DateTime validFrom,
            DateTime validTo,
            decimal minimumOrderAmount,
            Guid? applicableStoreId,
            string? description,
            Guid createdBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing discount.
        /// </summary>
        Task<Domain.Entities.Catalog.Discount> UpdateDiscountAsync(
            Guid discountId,
            string? code,
            DiscountType? type,
            decimal? value,
            DateTime? validFrom,
            DateTime? validTo,
            decimal? minimumOrderAmount,
            string? description,
            Guid updatedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a discount (sets IsActive = false).
        /// </summary>
        Task<bool> DeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a discount by ID.
        /// </summary>
        Task<Domain.Entities.Catalog.Discount?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a discount by its code.
        /// </summary>
        Task<Domain.Entities.Catalog.Discount?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all discounts for a specific store.
        /// </summary>
        Task<List<Domain.Entities.Catalog.Discount>> GetStoreDiscountsAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all global discounts (not store-specific).
        /// </summary>
        Task<List<Domain.Entities.Catalog.Discount>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts by type.
        /// </summary>
        Task<List<Domain.Entities.Catalog.Discount>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts expiring within specified days.
        /// </summary>
        Task<List<Domain.Entities.Catalog.Discount>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active discounts.
        /// </summary>
        Task<List<Domain.Entities.Catalog.Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default);
    }
}