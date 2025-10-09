using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Service interface for store authorization operations
    /// Provides methods for checking store permissions and user privileges
    /// </summary>
    public interface IStoreAuthorizationService
    {
        /// <summary>
        /// Checks if a user has admin privileges
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user is admin, false otherwise</returns>
        Task<bool> IsUserAdminAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user can perform an action on a store (owner or admin)
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="storeId">The store ID to check permissions for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can manage the store, false otherwise</returns>
        Task<bool> CanUserManageStoreAsync(Guid userId, Guid storeId, CancellationToken cancellationToken = default);
    }
}
