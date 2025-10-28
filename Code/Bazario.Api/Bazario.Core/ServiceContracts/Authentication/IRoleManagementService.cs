using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Service interface for role management operations
    /// Handles user role assignment and validation using ASP.NET Core Identity
    /// Accesses database via UserManager/RoleManager for authentication purposes
    /// </summary>
    public interface IRoleManagementService
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
