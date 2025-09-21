using Bazario.Core.DTO.Auth;

namespace Bazario.Core.ServiceContracts.Auth
{
    /// <summary>
    /// Service contract for user management operations
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Gets current user information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User result with success/failure status and data</returns>
        Task<UserResult> GetCurrentUserAsync(Guid userId);

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Password change request</param>
        /// <returns>Success status</returns>
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
