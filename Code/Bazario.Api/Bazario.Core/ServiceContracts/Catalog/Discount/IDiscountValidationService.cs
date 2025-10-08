using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.ServiceContracts.Catalog.Discount
{
    /// <summary>
    /// Service contract for discount validation business rules.
    /// Handles validation of discount codes for order application.
    /// </summary>
    public interface IDiscountValidationService
    {
        /// <summary>
        /// Validates if a discount code can be applied to an order.
        /// </summary>
        /// <param name="code">Discount code to validate</param>
        /// <param name="orderSubtotal">Order subtotal amount</param>
        /// <param name="storeIds">Store IDs in the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with discount details if valid</returns>
        Task<(bool IsValid, Domain.Entities.Catalog.Discount? Discount, string? ErrorMessage)> ValidateDiscountCodeAsync(
            string code,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates multiple discount codes for an order.
        /// </summary>
        /// <param name="codes">List of discount codes to validate</param>
        /// <param name="orderSubtotal">Order subtotal amount</param>
        /// <param name="storeIds">Store IDs in the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of valid discounts and list of error messages for invalid codes</returns>
        Task<(List<Domain.Entities.Catalog.Discount> ValidDiscounts, List<string> ErrorMessages)> ValidateMultipleDiscountCodesAsync(
            List<string> codes,
            decimal orderSubtotal,
            List<Guid> storeIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a discount code exists.
        /// </summary>
        Task<bool> DiscountExistsAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a discount code is unique (for creation/update).
        /// </summary>
        Task<bool> IsDiscountCodeUniqueAsync(string code, Guid? excludeDiscountId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates discount value based on type.
        /// </summary>
        /// <param name="type">Discount type</param>
        /// <param name="value">Discount value</param>
        /// <returns>True if value is valid for the type</returns>
        bool ValidateDiscountValue(Enums.Catalog.DiscountType type, decimal value);

        /// <summary>
        /// Validates discount date range.
        /// </summary>
        /// <param name="validFrom">Start date</param>
        /// <param name="validTo">End date</param>
        /// <returns>True if date range is valid</returns>
        bool ValidateDateRange(DateTime validFrom, DateTime validTo);

        /// <summary>
        /// Marks a discount as used (for one-time use discounts).
        /// </summary>
        Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default);
    }
}
