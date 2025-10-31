using Bazario.Core.ServiceContracts;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Bazario.Core.ServiceContracts.Infrastructure;

namespace Bazario.Core.Services.Infrastructure
{
    /// <summary>
    /// Production-ready email service using MailKit and SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailTemplateService _templateService;
        private readonly IEmailSender _emailSender;

        public EmailService(
            ILogger<EmailService> logger, 
            UserManager<ApplicationUser> userManager,
            IEmailTemplateService templateService,
            IEmailSender emailSender)
        {
            _logger = logger;
            _userManager = userManager;
            _templateService = templateService;
            _emailSender = emailSender;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string resetUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail) || 
                    string.IsNullOrWhiteSpace(userName) ||
                    string.IsNullOrWhiteSpace(resetToken) ||
                    string.IsNullOrWhiteSpace(resetUrl))
                {
                    _logger.LogWarning("Invalid parameters for SendPasswordResetEmailAsync");
                    return false;
                }

                var subject = "Password Reset Request - Bazario";
                var body = await _templateService.RenderPasswordResetEmailAsync(userName, resetUrl, resetToken);
                
                var result = await _emailSender.SendEmailAsync(toEmail, subject, body);
                
                if (result)
                {
                    _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}", toEmail);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken, string confirmationUrl)
        {
            try
            {
                var subject = "Confirm Your Email Address - Bazario";
                var body = await _templateService.RenderEmailConfirmationAsync(userName, confirmationUrl, confirmationToken);
                
                var result = await _emailSender.SendEmailAsync(toEmail, subject, body);
                
                if (result)
                {
                    _logger.LogInformation("Email confirmation sent successfully to {Email}", toEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to send email confirmation to {Email}", toEmail);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email confirmation to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Confirms user email using confirmation token
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
                    return false;
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for user {UserId}", userId);
                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Email confirmation failed for user {UserId}: {Errors}", userId, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email confirmation failed for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Sends a generic alert email with custom subject and HTML body
        /// </summary>
        public async Task<bool> SendGenericAlertEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail) ||
                    string.IsNullOrWhiteSpace(subject) ||
                    string.IsNullOrWhiteSpace(htmlBody))
                {
                    _logger.LogWarning("Invalid parameters for SendGenericAlertEmailAsync");
                    return false;
                }

                var result = await _emailSender.SendEmailAsync(toEmail, subject, htmlBody);

                if (result)
                {
                    _logger.LogInformation("Generic alert email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
                }
                else
                {
                    _logger.LogWarning("Failed to send generic alert email to {Email}", toEmail);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send generic alert email to {Email}", toEmail);
                return false;
            }
        }
    }
}
