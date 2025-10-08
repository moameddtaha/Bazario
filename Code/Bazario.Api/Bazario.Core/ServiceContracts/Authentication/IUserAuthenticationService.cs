using Bazario.Core.DTO.Authentication;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Service contract for user authentication operations
    /// </summary>
    public interface IUserAuthenticationService
    {
        /// <summary>
        /// Authenticates a user and generates tokens
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Authentication response with tokens</returns>
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
