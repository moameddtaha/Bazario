using Bazario.Core.DTO.Auth;
using Bazario.Core.Models.User;

namespace Bazario.Core.ServiceContracts
{
    /// <summary>
    /// Service contract for managing refresh tokens
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Stores a refresh token for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="refreshToken">Refresh token to store</param>
        /// <param name="accessTokenExpiresAt">When the access token expires</param>
        /// <param name="refreshTokenExpiresAt">When the refresh token expires</param>
        /// <returns>Success status</returns>
        Task<bool> StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt);
        
        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="refreshToken">Token to validate</param>
        /// <returns>User ID if valid, null if invalid</returns>
        Task<Guid?> ValidateRefreshTokenAsync(string refreshToken);
        
        /// <summary>
        /// Refreshes access token using refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token to use</param>
        /// <returns>New authentication response with tokens</returns>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        
        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        /// <param name="refreshToken">Token to revoke</param>
        /// <returns>Success status</returns>
        Task<bool> RevokeTokenAsync(string refreshToken);
        
        /// <summary>
        /// Revokes all refresh tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="revokedBy">Who revoked the tokens</param>
        /// <param name="reason">Reason for revocation</param>
        /// <returns>Success status</returns>
        Task<bool> RevokeAllUserTokensAsync(Guid userId, string revokedBy, string? reason = null);
        
        /// <summary>
        /// Cleans up expired tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();
    }
}
