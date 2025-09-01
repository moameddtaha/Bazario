using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Auth.Exceptions;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for user authentication operations
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<UserAuthenticationService> _logger;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IAdminRepository _adminRepository;

        public UserAuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            IConfiguration configuration,
            IRefreshTokenService refreshTokenService,
            ILogger<UserAuthenticationService> logger,
            ICustomerRepository customerRepository,
            ISellerRepository sellerRepository,
            IAdminRepository adminRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _configuration = configuration;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
            _customerRepository = customerRepository;
            _sellerRepository = sellerRepository;
            _adminRepository = adminRepository;
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
                await CheckEmailConfirmationStatusAsync(user);

                // Update last login and user data
                await UpdateUserLoginInfoAsync(user);

                // Generate tokens
                var roles = await _userManager.GetRolesAsync(user);
                var (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration) = await GenerateTokensAsync(user, roles);

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
                _logger.LogError(ex, "Login failed for email: {Email}. Error: {ErrorMessage}", request.Email, ex.Message);
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

        private async Task CheckEmailConfirmationStatusAsync(ApplicationUser user)
        {
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("User {Email} logged in with unconfirmed email. Consider prompting for email verification.", user.Email);
            }
        }

        private async Task UpdateUserLoginInfoAsync(ApplicationUser user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            var roles = await _userManager.GetRolesAsync(user);

            // Update role-specific user data
            if (roles.Contains("Customer"))
            {
                await _customerRepository.UpdateCustomerAsync(user);
            }
            else if (roles.Contains("Seller"))
            {
                await _sellerRepository.UpdateSellerAsync(user);
            }
            else if (roles.Contains("Admin"))
            {
                await _adminRepository.UpdateAdminAsync(user);
            }
        }

        private async Task<(string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration)> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
            );

            var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
            );

            await _refreshTokenService.StoreRefreshTokenAsync(user.Id, refreshToken, accessTokenExpiration, refreshTokenExpiration);

            return (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration);
        }

        private object CreateUserResponse(ApplicationUser user, List<string> roles)
        {
            try
            {
                return UserResponseHelper.CreateUserResponse(user, roles);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Role mapping failed during login for user {Email}: {Message}", user.Email, ex.Message);
                throw new BusinessRuleException("User role configuration error. Please contact support.", "RoleMappingFailed", ex);
            }
        }
    }
}
