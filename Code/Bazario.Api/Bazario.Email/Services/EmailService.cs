using Bazario.Core.ServiceContracts;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Bazario.Email.Models;
using Bazario.Email.ServiceContracts;

namespace Bazario.Email.Services
{
    /// <summary>
    /// Production-ready email service using MailKit and SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailSettings _emailSettings;
        private readonly EmailTemplateService _templateService;

        public EmailService(
            ILogger<EmailService> logger, 
            UserManager<ApplicationUser> userManager,
            IOptions<EmailSettings> emailSettings,
            EmailTemplateService templateService)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSettings = emailSettings.Value;
            _templateService = templateService;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string resetUrl)
        {
            try
            {
                var subject = "Password Reset Request - Bazario";
                var body = await _templateService.RenderPasswordResetEmailAsync(userName, resetUrl, resetToken);
                
                var result = await SendEmailAsync(toEmail, subject, body);
                
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
                
                var result = await SendEmailAsync(toEmail, subject, body);
                
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
        /// Sends an email using SMTP
        /// </summary>
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Validate email settings
                if (string.IsNullOrEmpty(_emailSettings.SmtpServer) || 
                    string.IsNullOrEmpty(_emailSettings.Username) || 
                    string.IsNullOrEmpty(_emailSettings.Password))
                {
                    _logger.LogError("Email settings are not properly configured");
                    return false;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                email.To.Add(new MailboxAddress("", toEmail));
                email.Subject = subject;

                // Create HTML body
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = body
                };

                using var smtp = new SmtpClient();
                
                // Configure SMTP client
                await smtp.ConnectAsync(
                    _emailSettings.SmtpServer, 
                    _emailSettings.SmtpPort, 
                    _emailSettings.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
                );

                // Authenticate
                await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                // Send email
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} via SMTP", toEmail);
                return false;
            }
        }

    }
}
