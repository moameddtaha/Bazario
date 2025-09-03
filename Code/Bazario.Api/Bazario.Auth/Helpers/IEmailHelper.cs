using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Interface for email operations
    /// </summary>
    public interface IEmailHelper
    {
        /// <summary>
        /// Sends email confirmation to a user
        /// </summary>
        Task<bool> SendConfirmationEmailAsync(ApplicationUser user);

        /// <summary>
        /// Sends password reset email to a user
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(ApplicationUser user, string token);
    }
}
