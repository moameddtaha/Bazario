using Bazario.Auth.DTO;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Auth.ServiceContracts;
using Bazario.Auth.Domain.Entities;
using Bazario.Auth.Domain.RepositoryContracts;


namespace Bazario.Auth.Services
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
                var tokenModel = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = userId,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt
                };

                await _refreshTokenRepository.CreateAsync(tokenModel);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Token storage failed (invalid argument): {UserId}", userId);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token storage failed (invalid operation): {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token storage failed: {UserId}", userId);
                return false;
            }
        }

        public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenModel = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (tokenModel == null)
                {
                    return null;
                }
                
                if (tokenModel.IsRevoked)
                {
                    _logger.LogWarning("Token revoked: {UserId}", tokenModel.UserId);
                    return null;
                }
                
                if (tokenModel.RefreshTokenExpiresAt <= DateTime.UtcNow)
                {
                    return null;
                }

                return tokenModel.UserId;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Token validation failed (invalid argument)");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token validation failed (invalid operation)");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Refreshes access token using refresh token
        /// </summary>
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Validate the refresh token
                var userId = await ValidateRefreshTokenAsync(refreshToken);
                if (userId == null)
                {
                    return AuthResponse.Failure("Invalid or expired refresh token.");
                }

                // Get the user
                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user == null)
                {
                    _logger.LogError("Token refresh failed: User {UserId} not found", userId.Value);
                    return AuthResponse.Failure("User not found.");
                }

                // Check if user is still active
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Token refresh failed: Account {UserId} is locked", userId.Value);
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
                    _logger.LogError(ex, "Role mapping failed: {UserId}", userId.Value);
                    return AuthResponse.Failure("User role configuration error. Please contact support.");
                }

                _logger.LogInformation("Tokens refreshed: {UserId}", userId.Value);
                
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
                _logger.LogError(ex, "Token refresh failed (invalid argument)");
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token refresh failed (invalid operation)");
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return AuthResponse.Failure($"Token refresh failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                // Revoke the refresh token
                var result = await RevokeRefreshTokenAsync(refreshToken, "User", "User requested revocation");
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Token revocation failed (invalid argument)");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token revocation failed (invalid operation)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation failed");
                return false;
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string revokedBy, string? reason = null)
        {
            try
            {
                var result = await _refreshTokenRepository.RevokeTokenAsync(refreshToken, revokedBy, reason);
                
                if (!result)
                {
                    _logger.LogWarning("Token revocation failed (token may not exist or already revoked)");
                }
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Token revocation failed (invalid argument)");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token revocation failed (invalid operation)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation failed");
                return false;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string revokedBy, string? reason = null)
        {
            try
            {
                var result = await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, revokedBy, reason);
                
                if (!result)
                {
                    _logger.LogWarning("No tokens revoked for user {UserId}", userId);
                }
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Revoke all tokens failed (invalid argument): {UserId}", userId);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Revoke all tokens failed (invalid operation): {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke all tokens failed: {UserId}", userId);
                return false;
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                // Delete expired tokens from database
                var deletedCount = await _refreshTokenRepository.DeleteExpiredAsync();
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {DeletedCount} expired tokens", deletedCount);
                }
                
                return deletedCount;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Token cleanup failed (invalid argument)");
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Token cleanup failed (invalid operation)");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token cleanup failed");
                return 0;
            }
        }
    }
}
