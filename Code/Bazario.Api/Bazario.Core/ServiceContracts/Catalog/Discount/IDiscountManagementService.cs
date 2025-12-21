using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Catalog.Discount;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.ServiceContracts.Catalog.Discount
{
    /// <summary>
    /// Service contract for managing discount CRUD operations.
    /// Handles creation, updating, retrieval, and deletion of discount codes.
    /// Uses DTOs for request/response to provide validation, security, and versioning.
    /// </summary>
    public interface IDiscountManagementService
    {
        /// <summary>
        /// Creates a new discount code.
        /// </summary>
        /// <param name="request">Discount creation request with validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created discount response with all properties</returns>
        Task<DiscountResponse> CreateDiscountAsync(
            DiscountAddRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing discount.
        /// Uses optimistic concurrency control via RowVersion.
        /// </summary>
        /// <param name="request">Discount update request with nullable properties for partial updates</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated discount response</returns>
        Task<DiscountResponse> UpdateDiscountAsync(
            DiscountUpdateRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a discount (sets IsActive = false).
        /// </summary>
        /// <param name="discountId">ID of discount to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a discount by ID.
        /// </summary>
        /// <param name="discountId">Discount ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Discount response or null if not found</returns>
        Task<DiscountResponse?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a discount by its code.
        /// </summary>
        /// <param name="code">Discount code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Discount response or null if not found</returns>
        Task<DiscountResponse?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all discounts for a specific store.
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discount responses</returns>
        Task<List<DiscountResponse>> GetStoreDiscountsAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all global discounts (not store-specific).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discount responses</returns>
        Task<List<DiscountResponse>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts by type.
        /// </summary>
        /// <param name="type">Discount type (Percentage or FixedAmount)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discount responses</returns>
        Task<List<DiscountResponse>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets discounts expiring within specified days.
        /// </summary>
        /// <param name="daysUntilExpiry">Number of days until expiry</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discount responses</returns>
        Task<List<DiscountResponse>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active discounts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discount responses</returns>
        Task<List<DiscountResponse>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default);
    }
}