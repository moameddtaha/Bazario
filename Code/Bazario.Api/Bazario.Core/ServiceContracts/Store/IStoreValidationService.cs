using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Validation operations for store business rules
    /// </summary>
    public interface IStoreValidationService
    {
        /// <summary>
        /// Validates if a store can be created for a seller
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <param name="storeName">Proposed store name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default);
    }
}
