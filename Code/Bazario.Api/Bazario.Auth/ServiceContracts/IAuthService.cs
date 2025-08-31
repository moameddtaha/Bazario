using Bazario.Auth.DTO;

namespace Bazario.Auth.ServiceContracts
{
    /// <summary>
    /// Service contract for authentication operations
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration request</param>
        /// <returns>Authentication response with tokens</returns>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Authenticates a user and generates tokens
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Authentication response with tokens</returns>
        Task<AuthResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Gets current user information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User information</returns>
        Task<object> GetCurrentUserAsync(Guid userId);

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Password change request</param>
        /// <returns>Success status</returns>
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        /// <summary>
        /// Initiates password reset process
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>Success status</returns>
        Task<bool> ForgotPasswordAsync(string email);

        /// <summary>
        /// Resets user password
        /// </summary>
        /// <param name="request">Password reset request</param>
        /// <returns>Success status</returns>
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
