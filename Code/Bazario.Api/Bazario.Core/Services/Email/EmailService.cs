using Bazario.Core.ServiceContracts;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Bazario.Core.Models.Email;

namespace Bazario.Core.Services.Email
{
    /// <summary>
    /// Production-ready email service using MailKit and SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailSettings _emailSettings;

        public EmailService(
            ILogger<EmailService> logger, 
            UserManager<ApplicationUser> userManager,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string resetUrl)
        {
            try
            {
                var subject = "Password Reset Request - Bazario";
                var body = GeneratePasswordResetEmailBody(userName, resetUrl, resetToken);
                
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
                var body = GenerateEmailConfirmationBody(userName, confirmationUrl, confirmationToken);
                
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

        /// <summary>
        /// Generates HTML body for password reset email
        /// </summary>
        private string GeneratePasswordResetEmailBody(string userName, string resetUrl, string resetToken)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 14px; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bazario</h1>
            <h2>Password Reset Request</h2>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>You have requested to reset your password. Please click the button below to reset your password:</p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}?token={resetToken}' class='button'>Reset Password</a>
            </div>
            
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{resetUrl}?token={resetToken}</p>
            
            <div class='warning'>
                <strong>Important:</strong> This link will expire in 1 hour for security reasons.
            </div>
            
            <p>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
            
            <p>Best regards,<br>The Bazario Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generates HTML body for email confirmation
        /// </summary>
        private string GenerateEmailConfirmationBody(string userName, string confirmationUrl, string confirmationToken)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirm Your Email Address</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 14px; }}
        .info {{ background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bazario</h1>
            <h2>Email Confirmation</h2>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>Welcome to Bazario! Please confirm your email address by clicking the button below:</p>
            
            <div style='text-align: center;'>
                <a href='{confirmationUrl}?token={confirmationToken}' class='button'>Confirm Email</a>
            </div>
            
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{confirmationUrl}?token={confirmationToken}</p>
            
            <div class='info'>
                <strong>Note:</strong> This link will expire in 24 hours.
            </div>
            
            <p>Once confirmed, you'll have full access to your Bazario account.</p>
            
            <p>Best regards,<br>The Bazario Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
