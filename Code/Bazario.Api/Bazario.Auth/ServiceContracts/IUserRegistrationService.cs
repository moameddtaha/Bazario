using Bazario.Auth.DTO;

namespace Bazario.Auth.ServiceContracts
{
    /// <summary>
    /// Service contract for user registration operations
    /// </summary>
    public interface IUserRegistrationService
    {
        /// <summary>
        /// Registers a new user with the specified role
        /// </summary>
        /// <param name="request">Registration request</param>
        /// <returns>Authentication response with tokens</returns>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
    }
}
