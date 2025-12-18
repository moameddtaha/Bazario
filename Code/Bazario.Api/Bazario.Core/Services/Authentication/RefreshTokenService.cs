using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Bazario.Core.Domain.Entities.Authentication;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Authentication;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.UserManagement;
using Bazario.Core.ServiceContracts.Authentication;

namespace Bazario.Core.Services.Authentication
{
    /// <summary>
    /// Database-based implementation of refresh token service
    /// Uses Unit of Work pattern for transaction management
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IRoleManagementService _roleManagementService;
        private readonly ILogger<RefreshTokenService> _logger;
        private readonly IConfiguration _configuration;


        public RefreshTokenService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IRoleManagementService roleManagementService,
            ILogger<RefreshTokenService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _jwtService = jwtService;
            _roleManagementService = roleManagementService;
            _logger = logger;
            _configuration = configuration;
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

                await _unitOfWork.RefreshTokens.CreateAsync(tokenModel);
                await _unitOfWork.SaveChangesAsync();
                return true;
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
                var tokenModel = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
                if (tokenModel == null)
                {
                    return null;
                }
                
                if (tokenModel.IsRevoked)
                {
                    // SECURITY: Token reuse detected - revoke ALL user tokens
                    // This follows OWASP best practices for detecting token theft
                    // If an attacker is using a revoked token, we assume the user's tokens are compromised
                    _logger.LogWarning("SECURITY ALERT: Revoked token reuse detected for UserId: {UserId}. Revoking all user tokens to prevent token theft.", tokenModel.UserId);

                    // Revoke all tokens for this user (token theft suspected)
                    await RevokeAllUserTokensAsync(
                        tokenModel.UserId,
                        "System",
                        "Token reuse detected - possible token theft"
                    );

                    return null;
                }
                
                if (tokenModel.RefreshTokenExpiresAt <= DateTime.UtcNow)
                {
                    return null;
                }

                return tokenModel.UserId;
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
                var roles = await _roleManagementService.GetUserRolesAsync(user);
                var (newAccessToken, newRefreshToken, accessTokenExpiration, refreshTokenExpiration) = GenerateTokens(user, roles);

                // BEGIN TRANSACTION - Critical section to prevent race conditions
                // Must revoke old token BEFORE storing new token to prevent replay attacks
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 1. Revoke old token FIRST (prevents replay attacks during token rotation)
                    var revokeResult = await RevokeRefreshTokenAsync(refreshToken, "System", "Replaced by new token");
                    if (!revokeResult)
                    {
                        // Token was already revoked or doesn't exist - possible replay attack
                        await _unitOfWork.RollbackTransactionAsync();
                        _logger.LogWarning("SECURITY: Token refresh failed - token already revoked. Possible replay attack. UserId: {UserId}", userId.Value);
                        return AuthResponse.Failure("Invalid refresh token.");
                    }

                    // 2. Store new token (only after old token is revoked)
                    var storeResult = await StoreRefreshTokenAsync(user.Id, newRefreshToken, accessTokenExpiration, refreshTokenExpiration);
                    if (!storeResult)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        _logger.LogError("Failed to store new refresh token: {UserId}", userId.Value);
                        return AuthResponse.Failure("Failed to store new refresh token.");
                    }

                    // Commit transaction - both revocation and storage succeeded
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Transaction failed during token refresh: {UserId}", userId.Value);
                    throw;
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed: {Message}", ex.Message);
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
                var result = await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken, revokedBy, reason);
                
                if (!result)
                {
                    _logger.LogWarning("Token revocation failed (token may not exist or already revoked)");
                }
                
                return result;
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
                var result = await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, revokedBy, reason);
                
                if (!result)
                {
                    _logger.LogWarning("No tokens revoked for user {UserId}", userId);
                }
                
                return result;
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
                var deletedCount = await _unitOfWork.RefreshTokens.DeleteExpiredAsync();
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {DeletedCount} expired tokens", deletedCount);
                }
                
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token cleanup failed");
                return 0;
            }
        }

        /// <summary>
        /// Generates access and refresh tokens for a user
        /// </summary>
        private (string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GenerateTokens(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
            );

            var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
            );

            return (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration);
        }
    }
}
