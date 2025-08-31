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
    /// Authentication service implementation
    /// Handles user registration, login, and core authentication operations
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;

        private readonly ILogger<AuthService> _logger;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IAdminRepository _adminRepository;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            IConfiguration configuration,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService,
            ILogger<AuthService> logger,
            ICustomerRepository customerRepository,
            ISellerRepository sellerRepository,
            IAdminRepository adminRepository
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _configuration = configuration;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
            _logger = logger;
            _customerRepository = customerRepository;
            _sellerRepository = sellerRepository;
            _adminRepository = adminRepository;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Starting user registration for email: {Email} with role: {Role}", request.Email, request.Role);
                
                // Validate role
                if (string.IsNullOrWhiteSpace(request.Role.ToString()))
                {
                    _logger.LogWarning("Registration failed: Role is required for email: {Email}", request.Email);
                    return AuthResponse.Failure("Role is required.");
                }

                // Validate role using enum
                if (request.Role != Role.Customer && request.Role != Role.Seller)
                {
                    _logger.LogWarning("Registration failed: Invalid role '{Role}' for email: {Email}. Allowed roles: Customer, Seller", request.Role, request.Email);
                    return AuthResponse.Failure("Invalid role. Role must be either 'Customer' or 'Seller'.");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: User already exists with email: {Email}", request.Email);
                    return AuthResponse.Failure("User with this email already exists.");
                }

                // Create new user
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

                _logger.LogInformation("Creating user account for email: {Email} with role: {Role}", request.Email, request.Role);
                
                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("User creation failed for email: {Email}. Errors: {Errors}", request.Email, string.Join(", ", errors));
                    return AuthResponse.Failure("Failed to create user.", errors);
                }
                
                _logger.LogInformation("User account created successfully for email: {Email} with ID: {UserId}", request.Email, user.Id);

                // Ensure the role exists before assigning it
                var roleName = request.Role.ToString();
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogInformation("Role '{Role}' does not exist, creating it now", roleName);

                    // Create the role if it doesn't exist
                    var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = roleResult.Errors.Select(e => e.Description).ToList();
                        _logger.LogError("Failed to create role '{Role}' for user {Email}. Errors: {Errors}", roleName, request.Email, string.Join(", ", roleErrors));
                        return AuthResponse.Failure($"Failed to create role '{roleName}'.", roleErrors);
                    }

                    _logger.LogInformation("Role '{Role}' created successfully", roleName);
                }

                // Add the selected role (Customer or Seller)
                _logger.LogInformation("Assigning role '{Role}' to user {Email}", roleName, request.Email);
                
                var roleAssignResult = await _userManager.AddToRoleAsync(user, roleName);

                if (!roleAssignResult.Succeeded)
                {
                    var roleErrors = roleAssignResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}", roleName, request.Email, string.Join(", ", roleErrors));
                    return AuthResponse.Failure($"Failed to assign role '{roleName}' to user.", roleErrors);
                }
                
                _logger.LogInformation("Role '{Role}' assigned successfully to user {Email}", roleName, request.Email);

                // Generate tokens
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken(user);

                // Calculate refresh token expiration
                var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
                );

                // Calculate access token expiration
                var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
                );

                // Store refresh token
                await _refreshTokenService.StoreRefreshTokenAsync(user.Id, refreshToken, accessTokenExpiration, refreshTokenExpiration);

                // Send confirmation email
                _logger.LogInformation("Sending confirmation email to user {Email}", request.Email);
                
                var emailSent = false;
                
                // Generate email confirmation token
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // In production, you would get this from configuration
                var confirmationUrl = _configuration["AppSettings:EmailConfirmationUrl"] ?? "https://yourapp.com/confirm-email";
                
                emailSent = await _emailService.SendEmailConfirmationAsync(user.Email, user.FirstName ?? user.UserName, confirmationToken, confirmationUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Confirmation email sent successfully to user {Email}", request.Email);
                }
                
                // Note: Email confirmation failure doesn't prevent registration
                // User can request email confirmation later if needed
                if (!emailSent)
                {
                    // Log email failure for monitoring
                    _logger.LogWarning("Failed to send confirmation email to user {UserId} at {Email}", user.Id, user.Email);
                }
                
                // Create role-specific user response
                object userResponse;
                try
                {
                    userResponse = UserResponseHelper.CreateUserResponse(user, roles.ToList());
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Role mapping failed during registration for user {Email}: {Message}", request.Email, ex.Message);
                    return AuthResponse.Failure("User role configuration error. Please contact support.");
                }

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

        /// <summary>
        /// Authenticates a user and generates tokens
        /// </summary>
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return AuthResponse.Failure("Invalid email or password.");
                }

                // Check if user is locked out
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return AuthResponse.Failure("Account is locked. Please try again later.");
                }

                // Attempt to sign in
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
                if (!result.Succeeded)
                {
                    return AuthResponse.Failure("Invalid email or password.");
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    // Log the email is not confirmed
                    _logger.LogWarning("User {Email} logged in with unconfirmed email. Consider prompting for email verification.", user.Email);
                    // Continue with login - user can verify email later
                }

                // Get roles
                var roles = await _userManager.GetRolesAsync(user);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;

                if (roles.Contains("Customer"))
                {
                    var customer = await _customerRepository.UpdateCustomerAsync(user);
                }
                else if (roles.Contains("Seller"))
                {
                    var seller = await _sellerRepository.UpdateSellerAsync(user);
                }
                else if (roles.Contains("Admin"))
                {
                    var admin = await _adminRepository.UpdateAdminAsync(user);
                }

                // Generate tokens
                
                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken(user);

                // Get access token expiration from configuration
                var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
                );

                // Get refresh token expiration from configuration
                var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
                );

                // Store refresh token
                await _refreshTokenService.StoreRefreshTokenAsync(user.Id, refreshToken, accessTokenExpiration, refreshTokenExpiration);

                // Create role-specific user response
                object userResponse;
                try
                {
                    userResponse = UserResponseHelper.CreateUserResponse(user, roles.ToList());
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Role mapping failed during login for user {Email}: {Message}", user.Email, ex.Message);
                    return AuthResponse.Failure("User role configuration error. Please contact support.");
                }

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
                return AuthResponse.Failure($"Login failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets current user information
        /// </summary>
        public async Task<object> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    // Return null if user not found
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);
                
                // Use the helper to get role-specific response
                try
                {
                    return UserResponseHelper.CreateUserResponse(user, roles.ToList());
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Role mapping failed when getting current user {UserId}: {Message}", userId, ex.Message);
                    return null; // Return null to indicate failure
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user for ID: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Changes user password
        /// </summary>
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
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initiates password reset process
        /// </summary>
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return false;
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Send password reset email
                var resetUrl = _configuration["AppSettings:PasswordResetUrl"] ?? "https://localhost:5001/reset-password";
                var userName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = user.Email ?? "User";
                }

                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    user.Email ?? "",
                    userName,
                    token,
                    resetUrl
                );

                return emailSent;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resets user password
        /// </summary>
        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return false;
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }
    }
}
