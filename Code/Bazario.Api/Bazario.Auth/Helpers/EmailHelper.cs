using Bazario.Core.Domain.IdentityEntities;
using Bazario.Email.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Helper class for email operations
    /// </summary>
    public class EmailHelper : IEmailHelper
    {
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailHelper> _logger;

        public EmailHelper(
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<EmailHelper> logger)
        {
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sends email confirmation to a user
        /// </summary>
        public async Task<bool> SendConfirmationEmailAsync(ApplicationUser user)
        {
            try
            {
                // Check if user has a valid email
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("Cannot send confirmation email: User {UserId} has no email", user.Id);
                    return false;
                }

                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationUrl = _configuration["AppSettings:EmailConfirmationUrl"] ?? "https://yourapp.com/confirm-email";
                
                // Get a valid user name for the email
                var userName = UserCreationHelper.GetUserNameForEmail(user);
                
                var emailSent = await _emailService.SendEmailConfirmationAsync(
                    user.Email, 
                    userName, 
                    confirmationToken, 
                    confirmationUrl);
                
                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send confirmation email: {Email}", user.Email);
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmation email failed: {Email}", user.Email ?? "unknown");
                return false;
            }
        }

        /// <summary>
        /// Sends password reset email to a user
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(ApplicationUser user, string token)
        {
            try
            {
                // Check if user has a valid email
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("Cannot send password reset email: User {UserId} has no email", user.Id);
                    return false;
                }

                var resetUrl = _configuration["AppSettings:PasswordResetUrl"] ?? "https://localhost:5001/reset-password";
                var userName = UserCreationHelper.GetUserNameForEmail(user);

                return await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    userName,
                    token,
                    resetUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset email failed: {Email}", user.Email ?? "unknown");
                return false;
            }
        }
    }
}
