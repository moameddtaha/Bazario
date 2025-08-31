using Bazario.Auth.Domain.Entities;
using Bazario.Auth.Domain.RepositoryContracts;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for refresh token operations
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("GetByTokenAsync called with null or empty token");
                return null;
            }

            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

                if (refreshToken == null)
                {
                    _logger.LogDebug("Refresh token not found for token: {TokenPrefix}...", token[..Math.Min(8, token.Length)]);
                }

                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh token for token: {TokenPrefix}...", 
                    token[..Math.Min(8, token.Length)]);
                throw;
            }
        }

        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetByUserIdAsync called with empty GUID");
                return Enumerable.Empty<RefreshToken>();
            }

            try
            {
                var refreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .OrderByDescending(rt => rt.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} active refresh tokens for user {UserId}", refreshTokens.Count, userId);
                return refreshTokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh tokens for user {UserId}", userId);
                throw;
            }
        }

        public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            if (string.IsNullOrWhiteSpace(refreshToken.Token))
                throw new ArgumentException("Refresh token value cannot be null or empty", nameof(refreshToken));

            if (refreshToken.UserId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(refreshToken));

            try
            {
                // Set default values
                refreshToken.Id = Guid.NewGuid();
                refreshToken.CreatedAt = DateTime.UtcNow;
                refreshToken.IsRevoked = false;
                
                await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
                var result = await _context.SaveChangesAsync(cancellationToken);
                
                if (result <= 0)
                {
                    throw new InvalidOperationException("Failed to save refresh token to database");
                }

                _logger.LogDebug("Created refresh token {TokenId} for user {UserId}", refreshToken.Id, refreshToken.UserId);
                return refreshToken;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while creating refresh token for user {UserId}", refreshToken.UserId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database constraint violation while creating refresh token for user {UserId}", refreshToken.UserId);
                throw;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Unexpected error while creating refresh token for user {UserId}", refreshToken.UserId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            if (refreshToken.Id == Guid.Empty)
                throw new ArgumentException("Refresh token ID cannot be empty", nameof(refreshToken));

            try
            {
                // Get the existing token from database to avoid updating unwanted fields
                var existingToken = await _context.RefreshTokens.FindAsync(new object[] { refreshToken.Id }, cancellationToken);
                if (existingToken == null)
                {
                    _logger.LogDebug("Attempted to update non-existent refresh token {TokenId}", refreshToken.Id);
                    return false;
                }
                
                // Update only the fields that should change
                existingToken.Token = refreshToken.Token;
                existingToken.AccessTokenExpiresAt = refreshToken.AccessTokenExpiresAt;
                existingToken.RefreshTokenExpiresAt = refreshToken.RefreshTokenExpiresAt;
                existingToken.IsRevoked = refreshToken.IsRevoked;
                existingToken.RevokedAt = refreshToken.RevokedAt;
                existingToken.RevokedBy = refreshToken.RevokedBy;
                existingToken.RevocationReason = refreshToken.RevocationReason;
                
                var result = await _context.SaveChangesAsync(cancellationToken);
                return result > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating refresh token {TokenId}", refreshToken.Id);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database constraint violation while updating refresh token {TokenId}", refreshToken.Id);
                throw;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Unexpected error while updating refresh token {TokenId}", refreshToken.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("DeleteAsync called with empty GUID");
                return false;
            }

            try
            {
                var token = await _context.RefreshTokens.FindAsync(new object[] { id }, cancellationToken);
                if (token == null)
                {
                    _logger.LogDebug("Attempted to delete non-existent refresh token {TokenId}", id);
                    return false;
                }
                
                _context.RefreshTokens.Remove(token);
                var result = await _context.SaveChangesAsync(cancellationToken);
                return result > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while deleting refresh token {TokenId}", id);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database constraint violation while deleting refresh token {TokenId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting refresh token {TokenId}", id);
                throw;
            }
        }

        public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredTokensQuery = _context.RefreshTokens
                    .Where(rt => rt.RefreshTokenExpiresAt <= DateTime.UtcNow);
                
                // Execute delete directly on the database for efficiency
                var deletedCount = await expiredTokensQuery.ExecuteDeleteAsync(cancellationToken);
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Deleted {Count} expired refresh tokens", deletedCount);
                }
                
                return deletedCount; // Return actual count for monitoring and control
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database constraint violation while deleting expired refresh tokens");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting expired refresh tokens");
                throw;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, string revokedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            if (string.IsNullOrWhiteSpace(revokedBy))
                throw new ArgumentException("RevokedBy cannot be null or empty", nameof(revokedBy));

            try
            {
                var refreshToken = await GetByTokenAsync(token, cancellationToken);
                if (refreshToken == null)
                {
                    _logger.LogDebug("Attempted to revoke non-existent refresh token: {TokenPrefix}...", token[..Math.Min(8, token.Length)]);
                    return false;
                }
                
                // Repository doesn't decide business logic - just perform the update
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedBy = revokedBy;
                refreshToken.RevocationReason = reason;
                
                return await UpdateAsync(refreshToken, cancellationToken);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error revoking refresh token: {TokenPrefix}...", token[..Math.Min(8, token.Length)]);
                throw;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string revokedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (string.IsNullOrWhiteSpace(revokedBy))
                throw new ArgumentException("RevokedBy cannot be null or empty", nameof(revokedBy));

            try
            {
                // Use batch update for better performance - avoids loading tokens into memory
                var result = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(rt => rt.IsRevoked, true)
                        .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                        .SetProperty(rt => rt.RevokedBy, revokedBy)
                        .SetProperty(rt => rt.RevocationReason, reason), cancellationToken);

                if (result > 0)
                {
                    _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId} by {RevokedBy}", result, userId, revokedBy);
                }
                else
                {
                    _logger.LogDebug("No active refresh tokens found for user {UserId} to revoke", userId);
                }
                
                return result > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while revoking all refresh tokens for user {UserId}", userId);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database constraint violation while revoking all refresh tokens for user {UserId}", userId);
                throw;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
                throw;
            }
        }
    }
}
