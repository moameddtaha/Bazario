using Bazario.Auth.Domain.Entities;
using Bazario.Core.Domain.Entities;

namespace Bazario.Auth.Domain.RepositoryContracts
{
    /// <summary>
    /// Repository contract for refresh token operations
    /// </summary>
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
        Task<bool> RevokeTokenAsync(string token, string revokedBy, string? reason = null, CancellationToken cancellationToken = default);
        Task<bool> RevokeAllUserTokensAsync(Guid userId, string revokedBy, string? reason = null, CancellationToken cancellationToken = default);
    }
}
