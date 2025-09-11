using Bazario.Core.Domain.IdentityEntities;
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
        #region Test Data Constants
        private const string TestEmail = "test@example.com";
        private const string TestUserName = "John Doe";
        private const string TestToken = "test-token-123";
        private const string TestUrl = "https://example.com/reset";
        private const string TestSubject = "Password Reset Request - Bazario";
        private const string TestHtmlBody = "<html>Password Reset Email</html>";
        private static readonly Guid TestUserId = Guid.NewGuid();
        #endregion

        #region Private Fields
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailTemplateService> _templateServiceMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        #endregion

        #region Constructor
        public EmailServiceTest()
        {
            _loggerMock = new Mock<ILogger<EmailService>>();
            _templateServiceMock = new Mock<IEmailTemplateService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _userManagerMock = CreateUserManagerMock();
        }
        #endregion

        #region Helper Methods
        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
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

        private EmailService CreateEmailService()
        {
            return new EmailService(
                _loggerMock.Object,
                _userManagerMock.Object,
                _templateServiceMock.Object,
                _emailSenderMock.Object);
        }
        #endregion

        #region SendPasswordResetEmailAsync Tests

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenEmailSent_ReturnsTrue()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            _emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, TestSubject, TestHtmlBody))
                .ReturnsAsync(true);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.True(result);
            _templateServiceMock.Verify(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(TestEmail, TestSubject, TestHtmlBody), Times.Once);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenEmailNotSent_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            _emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
            _templateServiceMock.Verify(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(TestEmail, TestSubject, TestHtmlBody), Times.Once);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenTemplateServiceThrowsException_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken))
                .ThrowsAsync(new Exception("Template rendering failed"));

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
            _templateServiceMock.Verify(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenEmailSenderThrowsException_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            _emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, TestSubject, TestHtmlBody))
                .ThrowsAsync(new Exception("SMTP connection failed"));

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
            _templateServiceMock.Verify(t => t.RenderPasswordResetEmailAsync(TestUserName, TestUrl, TestToken), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(TestEmail, TestSubject, TestHtmlBody), Times.Once);
        }

        [Theory]
        [InlineData(null, TestUserName, TestToken, TestUrl)]
        [InlineData("", TestUserName, TestToken, TestUrl)]
        [InlineData(TestEmail, null, TestToken, TestUrl)]
        [InlineData(TestEmail, "", TestToken, TestUrl)]
        [InlineData(TestEmail, TestUserName, null, TestUrl)]
        [InlineData(TestEmail, TestUserName, "", TestUrl)]
        [InlineData(TestEmail, TestUserName, TestToken, null)]
        [InlineData(TestEmail, TestUserName, TestToken, "")]
        public async Task SendPasswordResetEmailAsync_WithInvalidParameters_ReturnsFalse(string email, string userName, string token, string url)
        {
            // Arrange
            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendPasswordResetEmailAsync(email, userName, token, url);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SendEmailConfirmationAsync Tests

        [Fact]
        public async Task SendEmailConfirmationAsync_WhenEmailSent_ReturnsTrue()
        {
            // Arrange
            const string expectedSubject = "Confirm Your Email Address - Bazario";
            
            _templateServiceMock
                .Setup(t => t.RenderEmailConfirmationAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            _emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, expectedSubject, TestHtmlBody))
                .ReturnsAsync(true);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendEmailConfirmationAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.True(result);
            _templateServiceMock.Verify(t => t.RenderEmailConfirmationAsync(TestUserName, TestUrl, TestToken), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(TestEmail, expectedSubject, TestHtmlBody), Times.Once);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_WhenEmailNotSent_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderEmailConfirmationAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            _emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendEmailConfirmationAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_WhenTemplateServiceThrowsException_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderEmailConfirmationAsync(TestUserName, TestUrl, TestToken))
                .ThrowsAsync(new Exception("Template rendering failed"));

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendEmailConfirmationAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task SendEmailConfirmationAsync_WhenEmailSenderThrowsException_ReturnsFalse()
        {
            // Arrange
            _templateServiceMock
                .Setup(t => t.RenderEmailConfirmationAsync(TestUserName, TestUrl, TestToken))
                .ReturnsAsync(TestHtmlBody);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(es => es.SendEmailAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP server error"));

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.SendEmailConfirmationAsync(TestEmail, TestUserName, TestToken, TestUrl);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ConfirmEmailAsync Tests

        [Fact]
        public async Task ConfirmEmailAsync_WhenUserNotFound_ReturnsFalse()
        {
            // Arrange
            _userManagerMock
                .Setup(um => um.FindByIdAsync(TestUserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.ConfirmEmailAsync(TestUserId, TestToken);

            // Assert
            Assert.False(result);
            _userManagerMock.Verify(um => um.FindByIdAsync(TestUserId.ToString()), Times.Once);
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenConfirmationSucceeds_ReturnsTrue()
        {
            // Arrange
            var user = new ApplicationUser { Id = TestUserId };
            var identityResult = IdentityResult.Success;

            _userManagerMock
                .Setup(um => um.FindByIdAsync(TestUserId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.ConfirmEmailAsync(user, TestToken))
                .ReturnsAsync(identityResult);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.ConfirmEmailAsync(TestUserId, TestToken);

            // Assert
            Assert.True(result);
            _userManagerMock.Verify(um => um.FindByIdAsync(TestUserId.ToString()), Times.Once);
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(user, TestToken), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenConfirmationFails_ReturnsFalse()
        {
            // Arrange
            var user = new ApplicationUser { Id = TestUserId };
            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Invalid token" });

            _userManagerMock
                .Setup(um => um.FindByIdAsync(TestUserId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.ConfirmEmailAsync(user, TestToken))
                .ReturnsAsync(identityResult);

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.ConfirmEmailAsync(TestUserId, TestToken);

            // Assert
            Assert.False(result);
            _userManagerMock.Verify(um => um.FindByIdAsync(TestUserId.ToString()), Times.Once);
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(user, TestToken), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenExceptionThrown_ReturnsFalse()
        {
            // Arrange
            _userManagerMock
                .Setup(um => um.FindByIdAsync(TestUserId.ToString()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var emailService = CreateEmailService();

            // Act
            var result = await emailService.ConfirmEmailAsync(TestUserId, TestToken);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}