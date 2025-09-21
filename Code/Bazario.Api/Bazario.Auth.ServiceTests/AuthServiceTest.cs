using Bazario.Core.DTO.Auth;
using Bazario.Core.Enums;
using Bazario.Core.ServiceContracts.Auth;
using Bazario.Core.Services.Auth;
using Moq;

namespace Bazario.Auth.ServiceTests
{
    /// <summary>
    /// Unit tests for AuthService focusing on coordination logic and delegation.
    /// Individual service behaviors are tested in their respective test classes.
    /// </summary>
    public class AuthServiceTest
    {
        #region Test Data Constants
        private const string TestEmail = "test@example.com";
        private const string TestPassword = "TestPassword123!";
        private const string TestFirstName = "John";
        private const string TestLastName = "Doe";
        private static readonly Guid TestUserId = Guid.NewGuid();
        #endregion

        #region Private Fields
        private readonly Mock<IUserRegistrationService> _userRegistrationServiceMock;
        private readonly Mock<IUserAuthenticationService> _userAuthenticationServiceMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly Mock<IPasswordRecoveryService> _passwordRecoveryServiceMock;
        #endregion

        #region Constructor
        public AuthServiceTest()
        {
            _userRegistrationServiceMock = new Mock<IUserRegistrationService>();
            _userAuthenticationServiceMock = new Mock<IUserAuthenticationService>();
            _userManagementServiceMock = new Mock<IUserManagementService>();
            _passwordRecoveryServiceMock = new Mock<IPasswordRecoveryService>();
        }
        #endregion

        #region Helper Methods
        private AuthService CreateAuthService()
        {
            return new AuthService(
                _userRegistrationServiceMock.Object,
                _userAuthenticationServiceMock.Object,
                _userManagementServiceMock.Object,
                _passwordRecoveryServiceMock.Object);
        }

        private RegisterRequest CreateValidRegisterRequest()
        {
            return new RegisterRequest
            {
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                Password = TestPassword,
                ConfirmPassword = TestPassword,
                Role = Role.Customer,
                Gender = Gender.Male,
                Age = 25
            };
        }

        private LoginRequest CreateValidLoginRequest()
        {
            return new LoginRequest
            {
                Email = TestEmail,
                Password = TestPassword,
                RememberMe = false
            };
        }

        private AuthResponse CreateSuccessAuthResponse()
        {
            return AuthResponse.Success(
                "Success message",
                "access-token",
                "refresh-token",
                DateTime.UtcNow.AddMinutes(60),
                DateTime.UtcNow.AddDays(7),
                new { Id = TestUserId, Email = TestEmail }
            );
        }

        private AuthResponse CreateFailureAuthResponse()
        {
            return AuthResponse.Failure("Error message");
        }

        private UserResult CreateSuccessUserResult()
        {
            return new UserResult
            {
                IsSuccess = true,
                Message = "User retrieved successfully",
                User = new { Id = TestUserId, Email = TestEmail }
            };
        }

        private ChangePasswordRequest CreateValidChangePasswordRequest()
        {
            return new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };
        }

        private ResetPasswordRequest CreateValidResetPasswordRequest()
        {
            return new ResetPasswordRequest
            {
                Email = TestEmail,
                Token = "reset-token",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };
        }
        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidRequest_DelegatesToUserRegistrationService()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var expectedResponse = CreateSuccessAuthResponse();
            
            _userRegistrationServiceMock
                .Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(expectedResponse);

            var authService = CreateAuthService();

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            _userRegistrationServiceMock.Verify(s => s.RegisterAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenRegistrationFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var expectedResponse = CreateFailureAuthResponse();
            
            _userRegistrationServiceMock
                .Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(expectedResponse);

            var authService = CreateAuthService();

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.False(result.IsSuccess);
            _userRegistrationServiceMock.Verify(s => s.RegisterAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var authService = CreateAuthService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                authService.RegisterAsync(null!));
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidRequest_DelegatesToUserAuthenticationService()
        {
            // Arrange
            var request = CreateValidLoginRequest();
            var expectedResponse = CreateSuccessAuthResponse();
            
            _userAuthenticationServiceMock
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync(expectedResponse);

            var authService = CreateAuthService();

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            _userAuthenticationServiceMock.Verify(s => s.LoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenAuthenticationFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidLoginRequest();
            var expectedResponse = CreateFailureAuthResponse();
            
            _userAuthenticationServiceMock
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync(expectedResponse);

            var authService = CreateAuthService();

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            Assert.False(result.IsSuccess);
            _userAuthenticationServiceMock.Verify(s => s.LoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var authService = CreateAuthService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                authService.LoginAsync(null!));
        }

        #endregion

        #region GetCurrentUserAsync Tests

        [Fact]
        public async Task GetCurrentUserAsync_WithValidUserId_DelegatesToUserManagementService()
        {
            // Arrange
            var expectedResult = CreateSuccessUserResult();
            
            _userManagementServiceMock
                .Setup(s => s.GetCurrentUserAsync(TestUserId))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.GetCurrentUserAsync(TestUserId);

            // Assert
            Assert.Equal(expectedResult, result);
            _userManagementServiceMock.Verify(s => s.GetCurrentUserAsync(TestUserId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserAsync_WhenUserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var expectedResult = new UserResult
            {
                IsSuccess = false,
                Message = "User not found"
            };
            
            _userManagementServiceMock
                .Setup(s => s.GetCurrentUserAsync(TestUserId))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.GetCurrentUserAsync(TestUserId);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.False(result.IsSuccess);
            _userManagementServiceMock.Verify(s => s.GetCurrentUserAsync(TestUserId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithEmptyGuid_DelegatesToUserManagementService()
        {
            // Arrange
            var emptyUserId = Guid.Empty;
            var expectedResult = CreateSuccessUserResult();
            
            _userManagementServiceMock
                .Setup(s => s.GetCurrentUserAsync(emptyUserId))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.GetCurrentUserAsync(emptyUserId);

            // Assert
            Assert.Equal(expectedResult, result);
            _userManagementServiceMock.Verify(s => s.GetCurrentUserAsync(emptyUserId), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_WithValidRequest_DelegatesToUserManagementService()
        {
            // Arrange
            var request = CreateValidChangePasswordRequest();
            const bool expectedResult = true;
            
            _userManagementServiceMock
                .Setup(s => s.ChangePasswordAsync(TestUserId, request))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ChangePasswordAsync(TestUserId, request);

            // Assert
            Assert.Equal(expectedResult, result);
            _userManagementServiceMock.Verify(s => s.ChangePasswordAsync(TestUserId, request), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenPasswordChangeFails_ReturnsFalse()
        {
            // Arrange
            var request = CreateValidChangePasswordRequest();
            const bool expectedResult = false;
            
            _userManagementServiceMock
                .Setup(s => s.ChangePasswordAsync(TestUserId, request))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ChangePasswordAsync(TestUserId, request);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.False(result);
            _userManagementServiceMock.Verify(s => s.ChangePasswordAsync(TestUserId, request), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var authService = CreateAuthService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                authService.ChangePasswordAsync(TestUserId, null!));
        }

        #endregion

        #region ForgotPasswordAsync Tests

        [Fact]
        public async Task ForgotPasswordAsync_WithValidEmail_DelegatesToPasswordRecoveryService()
        {
            // Arrange
            const bool expectedResult = true;
            
            _passwordRecoveryServiceMock
                .Setup(s => s.ForgotPasswordAsync(TestEmail))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ForgotPasswordAsync(TestEmail);

            // Assert
            Assert.Equal(expectedResult, result);
            _passwordRecoveryServiceMock.Verify(s => s.ForgotPasswordAsync(TestEmail), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WhenPasswordRecoveryFails_ReturnsFalse()
        {
            // Arrange
            const bool expectedResult = false;
            
            _passwordRecoveryServiceMock
                .Setup(s => s.ForgotPasswordAsync(TestEmail))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ForgotPasswordAsync(TestEmail);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.False(result);
            _passwordRecoveryServiceMock.Verify(s => s.ForgotPasswordAsync(TestEmail), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ForgotPasswordAsync_WithInvalidEmail_DelegatesToPasswordRecoveryService(string invalidEmail)
        {
            // Arrange
            const bool expectedResult = false;
            
            _passwordRecoveryServiceMock
                .Setup(s => s.ForgotPasswordAsync(invalidEmail))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ForgotPasswordAsync(invalidEmail);

            // Assert
            Assert.Equal(expectedResult, result);
            _passwordRecoveryServiceMock.Verify(s => s.ForgotPasswordAsync(invalidEmail), Times.Once);
        }

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_WithValidRequest_DelegatesToPasswordRecoveryService()
        {
            // Arrange
            var request = CreateValidResetPasswordRequest();
            const bool expectedResult = true;
            
            _passwordRecoveryServiceMock
                .Setup(s => s.ResetPasswordAsync(request))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ResetPasswordAsync(request);

            // Assert
            Assert.Equal(expectedResult, result);
            _passwordRecoveryServiceMock.Verify(s => s.ResetPasswordAsync(request), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenPasswordResetFails_ReturnsFalse()
        {
            // Arrange
            var request = CreateValidResetPasswordRequest();
            const bool expectedResult = false;
            
            _passwordRecoveryServiceMock
                .Setup(s => s.ResetPasswordAsync(request))
                .ReturnsAsync(expectedResult);

            var authService = CreateAuthService();

            // Act
            var result = await authService.ResetPasswordAsync(request);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.False(result);
            _passwordRecoveryServiceMock.Verify(s => s.ResetPasswordAsync(request), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var authService = CreateAuthService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                authService.ResetPasswordAsync(null!));
        }

        #endregion

        #region Service Coordination Tests

        [Fact]
        public void AllServices_AreProperlyInjected()
        {
            // Arrange & Act
            var authService = CreateAuthService();

            // Assert
            Assert.NotNull(authService);
            // This test ensures the service can be created with all dependencies
            // Individual service behaviors are tested in their respective test classes
        }

        [Fact]
        public void AuthService_ImplementsIAuthService()
        {
            // Arrange & Act
            var authService = CreateAuthService();

            // Assert
            Assert.IsAssignableFrom<IAuthService>(authService);
        }

        #endregion
    }
}
