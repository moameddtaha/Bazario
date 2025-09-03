using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Interface for role management operations
    /// </summary>
    public interface IRoleManagementHelper
    {
        /// <summary>
        /// Ensures a role exists, creating it if necessary
        /// </summary>
        Task<bool> EnsureRoleExistsAsync(string roleName);

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        Task<bool> AssignRoleToUserAsync(ApplicationUser user, string roleName);

        /// <summary>
        /// Gets all roles for a user
        /// </summary>
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);

        /// <summary>
        /// Checks if a user has a specific role
        /// </summary>
        Task<bool> UserHasRoleAsync(ApplicationUser user, string roleName);
    }
}
