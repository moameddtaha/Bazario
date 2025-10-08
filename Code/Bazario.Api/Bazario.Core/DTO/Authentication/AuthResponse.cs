namespace Bazario.Core.DTO.Authentication
{
    /// <summary>
    /// Authentication response containing tokens and user information
    /// </summary>
    public class AuthResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiration { get; set; }
        public DateTime? RefreshTokenExpiration { get; set; }
        public object? User { get; set; }  // Changed from UserResponse? to object? to support different response types
        public List<string>? Errors { get; set; }

        public static AuthResponse Success(string message, string accessToken, string refreshToken, 
            DateTime accessTokenExpiration, DateTime refreshTokenExpiration, object user)
        {
            return new AuthResponse
            {
                IsSuccess = true,
                Message = message,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration,
                User = user
            };
        }

        public static AuthResponse Failure(string message, List<string>? errors = null)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }
    }
}
