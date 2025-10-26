using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Helpers.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Authorization
{
    /// <summary>
    /// Helper class for admin authorization operations using ASP.NET Core Identity roles
    /// </summary>
    public class AdminAuthorizationHelper : IAdminAuthorizationHelper
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRoleManagementHelper _roleManagementHelper;
        private readonly ILogger<AdminAuthorizationHelper> _logger;
        private const string AdminRoleName = "Admin";

        public AdminAuthorizationHelper(
            UserManager<ApplicationUser> userManager,
            IRoleManagementHelper roleManagementHelper,
            ILogger<AdminAuthorizationHelper> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManagementHelper = roleManagementHelper ?? throw new ArgumentNullException(nameof(roleManagementHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HasAdminPrivilegesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Empty user ID provided for admin privilege check");
                    return false;
                }

                _logger.LogDebug("Checking admin privileges for user {UserId}", userId);

                // Find user by ID
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                // Check if user has Admin role
                var isAdmin = await _roleManagementHelper.UserHasRoleAsync(user, AdminRoleName);

                if (isAdmin)
                {
                    _logger.LogInformation("User {UserId} ({Email}) has admin privileges", userId, user.Email);
                }
                else
                {
                    _logger.LogDebug("User {UserId} ({Email}) does not have admin privileges", userId, user.Email);
                }

                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin privileges for user {UserId}", userId);
                return false; // Fail safe - deny access on error
            }
        }

        public async Task ValidateAdminPrivilegesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            var hasAdminPrivileges = await HasAdminPrivilegesAsync(userId, cancellationToken);
            if (!hasAdminPrivileges)
            {
                _logger.LogWarning("User {UserId} attempted to perform admin operation without privileges", userId);
                throw new UnauthorizedAccessException($"User {userId} does not have admin privileges to perform this operation");
            }

            _logger.LogDebug("User {UserId} admin privileges validated successfully", userId);
        }
    }
}