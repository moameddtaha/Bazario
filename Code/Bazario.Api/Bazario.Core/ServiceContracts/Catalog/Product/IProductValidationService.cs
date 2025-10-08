using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Catalog.Product;

namespace Bazario.Core.ServiceContracts.Catalog.Product
{
    /// <summary>
    /// Service contract for product validation operations
    /// Handles product validation logic and business rules
    /// </summary>
    public interface IProductValidationService
    {
        /// <summary>
        /// Validates if a product can be ordered (stock, active status, etc.)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Desired quantity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ProductOrderValidation> ValidateForOrderAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    }
}
