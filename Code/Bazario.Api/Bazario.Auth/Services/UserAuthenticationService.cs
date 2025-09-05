using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Helpers;
using Bazario.Core.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Auth.Exceptions;
using Bazario.Auth.Helpers;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for user authentication operations
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserAuthenticationDependencies _deps;
        private readonly ILogger<UserAuthenticationService> _logger;

        public UserAuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserAuthenticationDependencies deps,
            ILogger<UserAuthenticationService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _deps = deps;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find and validate user
                var user = await FindAndValidateUserAsync(request);
                if (user == null)
                {
                    throw new AuthException("Invalid email or password.", AuthException.ErrorCodes.InvalidCredentials);
                }

                // Check if user is locked out
                if (await _userManager.IsLockedOutAsync(user))
                {
                    throw new AuthException("Account is locked. Please try again later.", AuthException.ErrorCodes.AccountLocked);
                }

                // Attempt to sign in
                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
                if (!signInResult.Succeeded)
                {
                    throw new AuthException("Invalid email or password.", AuthException.ErrorCodes.InvalidCredentials);
                }

                // Check email confirmation status
                CheckEmailConfirmationStatus(user);

                // Update last login and user data
                await UpdateUserLoginInfoAsync(user);

                // Generate tokens
                var roles = await _deps.RoleManagementHelper.GetUserRolesAsync(user);
                var (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration) = await _deps.TokenHelper.GenerateTokensAsync(user, roles);

                // Create user response
                var userResponse = CreateUserResponse(user, roles.ToList());

                return AuthResponse.Success(
                    "Login successful.",
                    accessToken,
                    refreshToken,
                    accessTokenExpiration,
                    refreshTokenExpiration,
                    userResponse
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed: {Email}", request.Email);
                return AuthResponse.Failure($"Login failed: {ex.Message}");
            }
        }

        private async Task<ApplicationUser?> FindAndValidateUserAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return null;
            }

            return user;
        }

        private void CheckEmailConfirmationStatus(ApplicationUser user)
        {
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Unconfirmed email login: {Email}", user.Email);
            }
        }

        private async Task UpdateUserLoginInfoAsync(ApplicationUser user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            var roles = await _deps.RoleManagementHelper.GetUserRolesAsync(user);

            // Update role-specific user data
            if (roles.Contains("Customer"))
            {
                await _deps.CustomerRepository.UpdateCustomerAsync(user);
            }
            else if (roles.Contains("Seller"))
            {
                await _deps.SellerRepository.UpdateSellerAsync(user);
            }
            else if (roles.Contains("Admin"))
            {
                await _deps.AdminRepository.UpdateAdminAsync(user);
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
