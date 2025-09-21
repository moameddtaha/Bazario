using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Helpers.Auth
{
    /// <summary>
    /// Interface for token-related operations
    /// </summary>
    public interface ITokenHelper
    {
        /// <summary>
        /// Generates access and refresh tokens for a user (without storing refresh token)
        /// </summary>
        (string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GenerateTokens(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// Gets token expiration times from configuration
        /// </summary>
        (DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GetTokenExpirationTimes();
    }
}
