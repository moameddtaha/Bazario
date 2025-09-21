using Bazario.Core.Domain.IdentityEntities;
using Microsoft.Extensions.Configuration;
using Bazario.Core.ServiceContracts.Auth;

namespace Bazario.Core.Helpers.Auth
{
    /// <summary>
    /// Helper class for token-related operations
    /// </summary>
    public class TokenHelper : ITokenHelper
    {
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public TokenHelper(
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates access and refresh tokens for a user (without storing refresh token)
        /// </summary>
        public (string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GenerateTokens(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            var (accessTokenExpiration, refreshTokenExpiration) = GetTokenExpirationTimes();

            return (accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration);
        }

        /// <summary>
        /// Gets token expiration times from configuration
        /// </summary>
        public (DateTime accessTokenExpiration, DateTime refreshTokenExpiration) GetTokenExpirationTimes()
        {
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
            );

            var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
            );

            return (accessTokenExpiration, refreshTokenExpiration);
        }
    }
}
