using Bazario.Core.DTO.Authentication;

namespace Bazario.Core.ServiceContracts.Authentication
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
