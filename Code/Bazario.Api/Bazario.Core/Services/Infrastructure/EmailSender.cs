using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit.Text;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using Bazario.Core.Models.Infrastructure;
using Bazario.Core.ServiceContracts.Infrastructure;

namespace Bazario.Core.Services.Infrastructure
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        private readonly ILogger<EmailSender> _logger;
        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
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
