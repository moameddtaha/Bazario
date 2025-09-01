using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Email.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for user registration operations
    /// </summary>
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserRegistrationService> _logger;

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            IConfiguration configuration,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService,
            ILogger<UserRegistrationService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _configuration = configuration;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Starting user registration for email: {Email} with role: {Role}", request.Email, request.Role);
                
                // Validate role
                if (!IsValidRole(request.Role))
                {
                    return AuthResponse.Failure("Invalid role. Role must be either 'Customer' or 'Seller'.");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return AuthResponse.Failure("User with this email already exists.");
                }

                // Create and save user
                var user = await CreateUserAsync(request);
                if (user == null)
                {
                    return AuthResponse.Failure("Failed to create user.");
                }

                // Ensure role exists and assign it
                var roleName = request.Role.ToString();
                if (!await EnsureRoleExistsAsync(roleName))
                {
                    return AuthResponse.Failure($"Failed to create role '{roleName}'.");
                }

                if (!await AssignRoleToUserAsync(user, roleName))
                {
                    return AuthResponse.Failure($"Failed to assign role '{roleName}' to user.");
                }

                // Generate tokens
                var roles = await _userManager.GetRolesAsync(user);
                var (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration) = await GenerateTokensAsync(user, roles);

                // Send confirmation email
                await SendConfirmationEmailAsync(user);

                // Create user response
                var userResponse = CreateUserResponse(user, roles.ToList());

                _logger.LogInformation("User registration completed successfully for email: {Email} with role: {Role}", request.Email, roleName);
                
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
                _logger.LogError(ex, "Registration failed unexpectedly for email: {Email}. Error: {ErrorMessage}", request.Email, ex.Message);
                return AuthResponse.Failure($"Registration failed: {ex.Message}");
            }
        }

        private bool IsValidRole(Role role)
        {
            return role == Role.Customer || role == Role.Seller;
        }

        private async Task<ApplicationUser?> CreateUserAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender?.ToString(),
                Age = request.Age,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("User creation failed for email: {Email}. Errors: {Errors}", request.Email, string.Join(", ", errors));
                return null;
            }

            _logger.LogInformation("User account created successfully for email: {Email} with ID: {UserId}", request.Email, user.Id);
            return user;
        }

        private async Task<bool> EnsureRoleExistsAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return true;
            }

            _logger.LogInformation("Role '{Role}' does not exist, creating it now", roleName);
            var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            
            if (!roleResult.Succeeded)
            {
                var roleErrors = roleResult.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to create role '{Role}'. Errors: {Errors}", roleName, string.Join(", ", roleErrors));
                return false;
            }

            _logger.LogInformation("Role '{Role}' created successfully", roleName);
            return true;
        }

        private async Task<bool> AssignRoleToUserAsync(ApplicationUser user, string roleName)
        {
            _logger.LogInformation("Assigning role '{Role}' to user {Email}", roleName, user.Email);
            
            var roleAssignResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleAssignResult.Succeeded)
            {
                var roleErrors = roleAssignResult.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}", roleName, user.Email, string.Join(", ", roleErrors));
                return false;
            }
            
            _logger.LogInformation("Role '{Role}' assigned successfully to user {Email}", roleName, user.Email);
            return true;
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

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            try
            {
                // Check if user has a valid email
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("Cannot send confirmation email: User {UserId} has no valid email address", user.Id);
                    return;
                }

                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationUrl = _configuration["AppSettings:EmailConfirmationUrl"] ?? "https://yourapp.com/confirm-email";
                
                // Get a valid user name for the email
                var userName = GetValidUserName(user);
                
                var emailSent = await _emailService.SendEmailConfirmationAsync(
                    user.Email, 
                    userName, 
                    confirmationToken, 
                    confirmationUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Confirmation email sent successfully to user {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send confirmation email to user {UserId} at {Email}", user.Id, user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation email to user {UserId} at {Email}", user.Id, user.Email ?? "unknown");
            }
        }

        private static string GetValidUserName(ApplicationUser user)
        {
            // Try to get a meaningful name, fallback to email, then to generic "User"
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                return user.FirstName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }
            
            return "User";
        }

        private object CreateUserResponse(ApplicationUser user, List<string> roles)
        {
            try
            {
                return UserResponseHelper.CreateUserResponse(user, roles);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Role mapping failed during registration for user {Email}: {Message}", user.Email, ex.Message);
                throw new InvalidOperationException("User role configuration error. Please contact support.");
            }
        }
    }
}
