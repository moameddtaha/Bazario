using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.ServiceContracts.Authorization
{
    /// <summary>
    /// Service interface for admin authorization operations
    /// Handles role-based authorization checks for administrative operations
    /// Accesses database via UserManager for authentication purposes
    /// </summary>
    public interface IAdminAuthorizationService
    {
        /// <summary>
        /// Checks if a user has admin privileges
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user has admin privileges, false otherwise</returns>
        Task<bool> HasAdminPrivilegesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a user has admin privileges and throws exception if not
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user does not have admin privileges</exception>
        Task ValidateAdminPrivilegesAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
