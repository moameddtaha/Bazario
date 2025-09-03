using Bazario.Core.Domain.IdentityEntities;
using Bazario.Auth.ServiceContracts;
using Microsoft.Extensions.Configuration;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Helper class for token-related operations
    /// </summary>
    public class TokenHelper : ITokenHelper
    {
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IConfiguration _configuration;

        public TokenHelper(
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates access and refresh tokens for a user
        /// </summary>
        public async Task<(string accessToken, string refreshToken, DateTime accessTokenExpiration, DateTime refreshTokenExpiration)> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")
            );

            var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7")
            );

            await _refreshTokenService.StoreRefreshTokenAsync(user.Id, refreshToken, accessTokenExpiration, refreshTokenExpiration);

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
