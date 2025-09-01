using Bazario.Auth.DTO;

namespace Bazario.Auth.ServiceContracts
{
    /// <summary>
    /// Service contract for password recovery operations
    /// </summary>
    public interface IPasswordRecoveryService
    {
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
