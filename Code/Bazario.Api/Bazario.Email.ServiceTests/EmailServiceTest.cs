using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Email.Models;
using Bazario.Email.ServiceContracts;
using Bazario.Email.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bazario.Email.ServiceTests
{
    public class EmailServiceTest
    {
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailTemplateService> _templateServiceMock;

        public EmailServiceTest()
        {
            _templateServiceMock = new Mock<IEmailTemplateService>();
            _loggerMock = new Mock<ILogger<EmailService>>();

            // Build fake UserManager<ApplicationUser>
            var store = new Mock<IUserStore<ApplicationUser>>();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        }

        #region SendPasswordResetEmailAsync Tests

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenEmailSent_ReturnsTrue()
        {
            // Arrange
            string toEmail = "test@example.com";
            string userName = "John";
            string resetToken = "token123";
            string resetUrl = "http://reset-url";

            _templateServiceMock
                .Setup(t => t.RenderPasswordResetEmailAsync(userName, resetUrl, resetToken))
                .ReturnsAsync("<html>Password Reset Email</html>");

            var emailSettings = Options.Create(new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                Username = "user",
                Password = "pass",
                FromEmail = "noreply@test.com",
                FromName = "Test",
                SmtpPort = 587,
                EnableSsl = true
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(es => es.SendEmailAsync(toEmail, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var emailService = new EmailService(
                _loggerMock.Object,
                _userManagerMock.Object,
                _templateServiceMock.Object,
                emailSenderMock.Object
                );

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(toEmail, userName, resetToken, resetUrl);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SendPasswordResetEmailAsync_WhenEmailNotSent_ReturnsFalse()
        {
        }

        [Fact]
        public void SendPasswordResetEmailAsync_WhenExceptionThrown_ReturnsFalse()
        {
        }

        #endregion
    }
}