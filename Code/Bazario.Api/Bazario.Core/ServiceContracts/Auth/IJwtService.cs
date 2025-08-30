using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.ServiceContracts
{
    /// <summary>
    /// Service contract for JWT token generation
    /// Token validation is handled by Microsoft.AspNetCore.Authentication.JwtBearer
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates JWT access token for a user
        /// </summary>
        /// <param name="user">User for whom to generate token</param>
        /// <param name="roles">User roles</param>
        /// <returns>Generated JWT token</returns>
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// Generates refresh token for a user
        /// </summary>
        /// <param name="user">User for whom to generate refresh token</param>
        /// <returns>Generated refresh token</returns>
        string GenerateRefreshToken(ApplicationUser user);
    }
}
