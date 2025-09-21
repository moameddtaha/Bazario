namespace Bazario.Core.ServiceContracts.Email
{
    /// <summary>
    /// Service contract for sending emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="resetToken">Password reset token</param>
        /// <param name="resetUrl">Password reset URL</param>
        /// <returns>Success status</returns>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string resetUrl);
        
        /// <summary>
        /// Sends an email confirmation email
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="confirmationToken">Email confirmation token</param>
        /// <param name="confirmationUrl">Email confirmation URL</param>
        /// <returns>Success status</returns>
        Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken, string confirmationUrl);

        /// <summary>
        /// Confirms user email using confirmation token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="token">Confirmation token</param>
        /// <returns>Success status</returns>
        Task<bool> ConfirmEmailAsync(Guid userId, string token);
    }
}
