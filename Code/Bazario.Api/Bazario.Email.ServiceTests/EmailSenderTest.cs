using Bazario.Core.Models.Infrastructure;
using Bazario.Core.Services.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bazario.Email.ServiceTests
{
    /// <summary>
    /// Unit tests for EmailSender focusing on validation logic and error handling.
    /// SMTP functionality testing is left for integration tests.
    /// </summary>
    public class EmailSenderTest
    {
        #region Test Data Constants
        private const string TestEmail = "test@example.com";
        private const string TestSubject = "Test Subject";
        private const string TestBody = "<html>Test Body</html>";
        private const string TestSmtpServer = "smtp.test.com";
        private const int TestSmtpPort = 587;
        private const string TestUsername = "testuser";
        private const string TestPassword = "testpass";
        private const string TestFromEmail = "noreply@test.com";
        private const string TestFromName = "Test Sender";
        #endregion

        #region Private Fields
        private readonly Mock<ILogger<EmailSender>> _loggerMock;
        private readonly Mock<IOptions<EmailSettings>> _emailSettingsMock;
        #endregion

        #region Constructor
        public EmailSenderTest()
        {
            _loggerMock = new Mock<ILogger<EmailSender>>();
            _emailSettingsMock = new Mock<IOptions<EmailSettings>>();
        }
        #endregion

        #region Helper Methods
        private EmailSettings CreateValidEmailSettings()
        {
            return new EmailSettings
            {
                SmtpServer = TestSmtpServer,
                SmtpPort = TestSmtpPort,
                Username = TestUsername,
                Password = TestPassword,
                FromEmail = TestFromEmail,
                FromName = TestFromName,
                EnableSsl = true
            };
        }

        private EmailSender CreateEmailSender(EmailSettings? settings = null)
        {
            var emailSettings = settings ?? CreateValidEmailSettings();
            _emailSettingsMock.Setup(x => x.Value).Returns(emailSettings);
            return new EmailSender(_emailSettingsMock.Object, _loggerMock.Object);
        }
        #endregion

        #region Email Settings Validation Tests

        [Fact]
        public async Task SendEmailAsync_WithNullSmtpServer_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.SmtpServer = null!;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptySmtpServer_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.SmtpServer = string.Empty;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithNullUsername_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.Username = null!;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyUsername_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.Username = string.Empty;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithNullPassword_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.Password = null!;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyPassword_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = CreateValidEmailSettings();
            invalidSettings.Password = string.Empty;
            var emailSender = CreateEmailSender(invalidSettings);

            // Act
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithValidSettings_DoesNotThrowException()
        {
            // Arrange
            var emailSender = CreateEmailSender();

            // Act & Assert
            // This test ensures the method doesn't throw exceptions with valid settings
            // The actual SMTP sending will be tested in integration tests
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        #endregion

        #region Parameter Validation Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendEmailAsync_WithInvalidEmail_DoesNotThrowException(string invalidEmail)
        {
            // Arrange
            var emailSender = CreateEmailSender();

            // Act & Assert
            // EmailSender doesn't validate email format - that's handled by SMTP server
            var result = await emailSender.SendEmailAsync(invalidEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendEmailAsync_WithInvalidSubject_DoesNotThrowException(string invalidSubject)
        {
            // Arrange
            var emailSender = CreateEmailSender();

            // Act & Assert
            // EmailSender doesn't validate subject - that's handled by SMTP server
            var result = await emailSender.SendEmailAsync(TestEmail, invalidSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendEmailAsync_WithInvalidBody_DoesNotThrowException(string invalidBody)
        {
            // Arrange
            var emailSender = CreateEmailSender();

            // Act & Assert
            // EmailSender doesn't validate body - that's handled by SMTP server
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, invalidBody);
            Assert.IsType<bool>(result);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task SendEmailAsync_WithSslEnabled_DoesNotThrowException()
        {
            // Arrange
            var sslSettings = CreateValidEmailSettings();
            sslSettings.EnableSsl = true;
            var emailSender = CreateEmailSender(sslSettings);

            // Act & Assert
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithSslDisabled_DoesNotThrowException()
        {
            // Arrange
            var noSslSettings = CreateValidEmailSettings();
            noSslSettings.EnableSsl = false;
            var emailSender = CreateEmailSender(noSslSettings);

            // Act & Assert
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithCustomPort_DoesNotThrowException()
        {
            // Arrange
            var customPortSettings = CreateValidEmailSettings();
            customPortSettings.SmtpPort = 465; // Alternative SMTP port
            var emailSender = CreateEmailSender(customPortSettings);

            // Act & Assert
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithCustomFromName_DoesNotThrowException()
        {
            // Arrange
            var customFromSettings = CreateValidEmailSettings();
            customFromSettings.FromName = "Custom Sender Name";
            var emailSender = CreateEmailSender(customFromSettings);

            // Act & Assert
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithCustomFromEmail_DoesNotThrowException()
        {
            // Arrange
            var customFromSettings = CreateValidEmailSettings();
            customFromSettings.FromEmail = "custom@example.com";
            var emailSender = CreateEmailSender(customFromSettings);

            // Act & Assert
            var result = await emailSender.SendEmailAsync(TestEmail, TestSubject, TestBody);
            Assert.IsType<bool>(result);
        }

        #endregion
    }
}