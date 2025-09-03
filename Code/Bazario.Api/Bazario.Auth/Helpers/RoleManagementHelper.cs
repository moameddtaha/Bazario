using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Helper class for role management operations
    /// </summary>
    public class RoleManagementHelper : IRoleManagementHelper
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleManagementHelper(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Ensures a role exists, creating it if necessary
        /// </summary>
        public async Task<bool> EnsureRoleExistsAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return true;
            }

            var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            return roleResult.Succeeded;
        }

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        public async Task<bool> AssignRoleToUserAsync(ApplicationUser user, string roleName)
        {
            var roleAssignResult = await _userManager.AddToRoleAsync(user, roleName);
            return roleAssignResult.Succeeded;
        }

        /// <summary>
        /// Gets all roles for a user
        /// </summary>
        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        /// <summary>
        /// Checks if a user has a specific role
        /// </summary>
        public async Task<bool> UserHasRoleAsync(ApplicationUser user, string roleName)
        {
            return await _userManager.IsInRoleAsync(user, roleName);
        }
    }
}
