using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Interface for token-related operations
    /// </summary>
    public interface ITokenHelper
    {
        /// <summary>
        /// Generates access and refresh tokens for a user
        /// </summary>
        Task<(string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration)> GenerateTokensAsync(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// Gets token expiration times from configuration
        /// </summary>
        (DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GetTokenExpirationTimes();
    }
}
