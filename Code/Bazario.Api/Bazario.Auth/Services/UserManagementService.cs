using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Auth.Exceptions;

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
                _logger.LogError(ex, "GetCurrentUser failed: {UserId}", userId);
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
                    throw new AuthException("User not found.", AuthException.ErrorCodes.UserNotFound);
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    throw new ValidationException("Password change failed. Please check your current password.", "PasswordChange", errors);
                }
                
                return true;
            }
            catch (AuthException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (ValidationException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed: {UserId}", userId);
                throw new AuthException("Failed to change password.", AuthException.ErrorCodes.ValidationError, ex);
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
                _logger.LogError(ex, "Role mapping failed: {UserId}", user.Id);
                throw new BusinessRuleException("User role configuration error. Please contact support.", "RoleMappingFailed", ex);
            }
        }
    }
}
