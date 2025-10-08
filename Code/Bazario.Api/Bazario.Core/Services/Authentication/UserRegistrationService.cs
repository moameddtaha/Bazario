using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using ValidationException = Bazario.Core.Exceptions.Shared.ValidationException;
using Bazario.Core.Exceptions.Shared;
using Bazario.Core.DTO.Authentication;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.UserManagement;
using Bazario.Core.Models.UserManagement;
using Bazario.Core.ServiceContracts.Authentication;



namespace Bazario.Core.Services.Authentication
{
    /// <summary>
    /// Service for user registration operations
    /// </summary>
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRegistrationDependencies _deps;
        private readonly ILogger<UserRegistrationService> _logger;

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            IUserRegistrationDependencies deps,
            ILogger<UserRegistrationService> logger)
        {
            _userManager = userManager;
            _deps = deps;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Validate request is not null
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Registration request cannot be null.");
                }

                _logger.LogInformation("User registration started: {Email} ({Role})", request.Email, request.Role);
                
                // Validate role
                if (!UserCreationHelper.IsValidRole(request.Role))
                {
                    throw new ValidationException("Invalid role. Role must be either 'Customer' or 'Seller'.", "Role", request.Role);
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    throw new BusinessRuleException("User with this email already exists.", "UniqueEmailRule");
                }

                // Create and save user
                var user = await _deps.UserCreationService.CreateUserAsync(request);
                if (user == null)
                {
                    throw new BusinessRuleException("Failed to create user account.", "UserCreationFailed");
                }

                // Ensure role exists and assign it
                var roleName = request.Role.ToString();
                if (!await _deps.RoleManagementHelper.EnsureRoleExistsAsync(roleName))
                {
                    throw new BusinessRuleException($"Failed to create role '{roleName}'.", "RoleCreationFailed");
                }

                if (!await _deps.RoleManagementHelper.AssignRoleToUserAsync(user, roleName))
                {
                    throw new BusinessRuleException($"Failed to assign role '{roleName}' to user.", "RoleAssignmentFailed");
                }

                // Generate tokens
                var roles = await _deps.RoleManagementHelper.GetUserRolesAsync(user);
                var (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration) = _deps.TokenHelper.GenerateTokens(user, roles);
                
                // Store refresh token
                await _deps.RefreshTokenService.StoreRefreshTokenAsync(user.Id, refreshToken, accessTokenExpiration, refreshTokenExpiration);

                // Send confirmation email
                await _deps.EmailHelper.SendConfirmationEmailAsync(user);

                // Create user response
                var userResponse = CreateUserResponse(user, roles.ToList());

                _logger.LogInformation("User registration completed: {Email} ({Role})", request.Email, roleName);
                
                return AuthResponse.Success(
                    $"User registered successfully as {roleName}. Please check your email to confirm your account before logging in.",
                    accessToken,
                    refreshToken,
                    accessTokenExpiration,
                    refreshTokenExpiration,
                    userResponse
                );
            }
            catch (Exception ex)
            {
                var email = request?.Email ?? "N/A"; // Safe fallback
                _logger.LogError(ex, "Registration failed: {Email}", email);

                return AuthResponse.Failure($"Registration failed: {ex.Message}");
            }
        }

        private UserResponse CreateUserResponse(ApplicationUser user, List<string> roles)
        {
            try
            {
                return UserResponseHelper.CreateUserResponse(user, roles);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Role mapping failed: {Email}", user.Email);
                throw new BusinessRuleException("User role configuration error. Please contact support.", "RoleMappingFailed", ex);
            }
        }
    }
}
