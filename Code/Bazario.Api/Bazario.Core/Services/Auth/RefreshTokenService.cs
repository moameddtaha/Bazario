using Bazario.Core.ServiceContracts;
using Bazario.Core.DTO.Auth;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Bazario.Core.Services
{
    /// <summary>
    /// Database-based implementation of refresh token service
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshTokenService> _logger;


        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IConfiguration configuration,
            ILogger<RefreshTokenService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _jwtService = jwtService;
            _configuration = configuration;
            _logger = logger;

        }

        public async Task<bool> StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt)
        {
            try
            {
                _logger.LogInformation("Storing refresh token for user {UserId}", userId);
                
                var tokenModel = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = userId,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt
                };

                await _refreshTokenRepository.CreateAsync(tokenModel);
                _logger.LogInformation("Successfully stored refresh token for user {UserId}", userId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during refresh token storage for user {UserId}: {Message}", userId, ex.Message);
                return false; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during refresh token storage for user {UserId}: {Message}", userId, ex.Message);
                return false; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error during refresh token storage for user {UserId}. Exception type: {ExceptionType}, Message: {Message}", 
                    userId, ex.GetType().Name, ex.Message);
                return false; // Database or unexpected error
            }
        }

        public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogDebug("Validating refresh token: {TokenPrefix}...", refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                
                var tokenModel = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (tokenModel == null)
                {
                    _logger.LogWarning("Refresh token not found: {TokenPrefix}...", refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return null;
                }
                
                if (tokenModel.IsRevoked)
                {
                    _logger.LogWarning("Refresh token is revoked for user {UserId}: {TokenPrefix}...", tokenModel.UserId, refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return null;
                }
                
                if (tokenModel.RefreshTokenExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Refresh token expired for user {UserId}: {TokenPrefix}...", tokenModel.UserId, refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return null;
                }

                _logger.LogDebug("Refresh token validated successfully for user {UserId}", tokenModel.UserId);
                return tokenModel.UserId;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during refresh token validation: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return null; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during refresh token validation: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return null; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error during refresh token validation: {TokenPrefix}... Exception type: {ExceptionType}, Message: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.GetType().Name, ex.Message);
                return null; // Database or unexpected error
            }
        }

        /// <summary>
        /// Refreshes access token using refresh token
        /// </summary>
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Starting token refresh process for token: {TokenPrefix}...", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                
                // Validate the refresh token
                var userId = await ValidateRefreshTokenAsync(refreshToken);
                if (userId == null)
                {
                    _logger.LogWarning("Token refresh failed: Invalid or expired refresh token: {TokenPrefix}...", 
                        refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return AuthResponse.Failure("Invalid or expired refresh token.");
                }

                // Get the user
                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user == null)
                {
                    _logger.LogError("Token refresh failed: User {UserId} not found for valid refresh token: {TokenPrefix}...", 
                        userId.Value, refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return AuthResponse.Failure("User not found.");
                }

                // Check if user is still active
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Token refresh failed: Account {UserId} is locked. Token: {TokenPrefix}...", 
                        userId.Value, refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                    return AuthResponse.Failure("Account is locked.");
                }

                // Generate new tokens
                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken(user);

                // Calculate expiration times
                var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
                );
                var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
                );
                
                // Store the new refresh token
                await StoreRefreshTokenAsync(user.Id, newRefreshToken, accessTokenExpiration, refreshTokenExpiration);

                // Revoke the old refresh token
                await RevokeRefreshTokenAsync(refreshToken, "System", "Replaced by new token");

                // Create user response based on role
                object userResponse;
                try
                {
                    userResponse = UserResponseHelper.CreateUserResponse(user, roles.ToList());
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Role mapping failed during token refresh for user {UserId}: {Message}", userId.Value, ex.Message);
                    return AuthResponse.Failure("User role configuration error. Please contact support.");
                }

                _logger.LogInformation("Successfully refreshed tokens for user {UserId}. Old token: {OldTokenPrefix}..., New token: {NewTokenPrefix}...", 
                    userId.Value, 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)),
                    newRefreshToken.Substring(0, Math.Min(8, newRefreshToken.Length)));
                
                return AuthResponse.Success(
                    "Token refreshed successfully.",
                    newAccessToken,
                    newRefreshToken,
                    accessTokenExpiration,
                    refreshTokenExpiration,
                    userResponse
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during token refresh: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}"); // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during token refresh: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}"); // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error during token refresh: {TokenPrefix}... Exception type: {ExceptionType}, Message: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.GetType().Name, ex.Message);
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}"); // Database or unexpected error
            }
        }

        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("User requested revocation of refresh token: {TokenPrefix}...", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                
                // Revoke the refresh token
                var result = await RevokeRefreshTokenAsync(refreshToken, "User", "User requested revocation");
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during user-requested token revocation: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return false; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during user-requested token revocation: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return false; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error during user-requested token revocation: {TokenPrefix}... Exception type: {ExceptionType}, Message: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.GetType().Name, ex.Message);
                return false; // Database or unexpected error
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string revokedBy, string? reason = null)
        {
            try
            {
                _logger.LogInformation("Revoking refresh token: {TokenPrefix}... by {RevokedBy}. Reason: {Reason}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), revokedBy, reason ?? "No reason provided");
                
                var result = await _refreshTokenRepository.RevokeTokenAsync(refreshToken, revokedBy, reason);
                
                if (result)
                {
                    _logger.LogInformation("Successfully revoked refresh token: {TokenPrefix}...", refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                }
                else
                {
                    _logger.LogWarning("Failed to revoke refresh token: {TokenPrefix}... (token may not exist or already revoked)", 
                        refreshToken.Substring(0, Math.Min(8, refreshToken.Length)));
                }
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while revoking refresh token: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return false; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while revoking refresh token: {TokenPrefix}... Error: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.Message);
                return false; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error while revoking refresh token: {TokenPrefix}... Exception type: {ExceptionType}, Message: {Message}", 
                    refreshToken.Substring(0, Math.Min(8, refreshToken.Length)), ex.GetType().Name, ex.Message);
                return false; // Database or unexpected error
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string revokedBy, string? reason = null)
        {
            try
            {
                _logger.LogInformation("Revoking all refresh tokens for user {UserId} by {RevokedBy}. Reason: {Reason}", 
                    userId, revokedBy, reason ?? "No reason provided");
                
                var result = await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, revokedBy, reason);
                
                if (result)
                {
                    _logger.LogInformation("Successfully revoked all refresh tokens for user {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("No refresh tokens were revoked for user {UserId} (user may not have any active tokens)", userId);
                }
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while revoking all tokens for user {UserId}: {Message}", userId, ex.Message);
                return false; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while revoking all tokens for user {UserId}: {Message}", userId, ex.Message);
                return false; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error while revoking all tokens for user {UserId}. Exception type: {ExceptionType}, Message: {Message}", 
                    userId, ex.GetType().Name, ex.Message);
                return false; // Database or unexpected error
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of expired refresh tokens");
                
                // Delete expired tokens from database
                var deletedCount = await _refreshTokenRepository.DeleteExpiredAsync();
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Successfully cleaned up {DeletedCount} expired refresh tokens", deletedCount);
                }
                else
                {
                    _logger.LogDebug("No expired refresh tokens found during cleanup");
                }
                
                return deletedCount;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during cleanup of expired refresh tokens: {Message}", ex.Message);
                return 0; // Bad input data
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during cleanup of expired refresh tokens: {Message}", ex.Message);
                return 0; // Business rule violation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database or unexpected error during cleanup of expired refresh tokens. Exception type: {ExceptionType}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                return 0; // Database or unexpected error
            }
        }
    }
}
