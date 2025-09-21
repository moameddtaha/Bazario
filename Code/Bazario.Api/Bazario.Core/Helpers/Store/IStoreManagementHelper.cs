using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Interface for store management helper operations
    /// Provides methods for store permission checking and validation
    /// </summary>
    public interface IStoreManagementHelper
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
