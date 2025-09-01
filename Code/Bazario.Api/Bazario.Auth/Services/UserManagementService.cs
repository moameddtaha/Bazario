using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for user management operations
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<UserResult> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return UserResult.NotFound($"User with ID {userId} not found");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userResponse = CreateUserResponse(user, roles.ToList());
                
                return UserResult.Success(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user for ID: {UserId}", userId);
                return UserResult.Error($"Failed to retrieve user: {ex.Message}");
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return false;
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID: {UserId}", userId);
                return false;
            }
        }

        private object CreateUserResponse(ApplicationUser user, List<string> roles)
        {
            try
            {
                return UserResponseHelper.CreateUserResponse(user, roles);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Role mapping failed when getting current user {UserId}: {Message}", user.Id, ex.Message);
                return null;
            }
        }
    }
}
