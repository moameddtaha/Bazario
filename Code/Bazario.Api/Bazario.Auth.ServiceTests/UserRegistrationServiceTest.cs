using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.DTO.Authentication;
using Bazario.Core.Enums.Authentication;
using Bazario.Core.Models.UserManagement;
using Bazario.Core.ServiceContracts.Authentication;
using Bazario.Core.Services.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bazario.Auth.ServiceTests
{
    /// <summary>
    /// Unit tests for UserRegistrationService focusing on registration logic.
    /// All dependencies are mocked to ensure true unit testing.
    /// </summary>
    public class UserRegistrationServiceTest
    {
        #region Test Data Constants
        private const string TestEmail = "test@example.com";
        private const string TestPassword = "TestPassword123!";
        private const string TestFirstName = "John";
        private const string TestLastName = "Doe";
        private const string TestAccessToken = "access-token";
        private const string TestRefreshToken = "refresh-token";
        private static readonly Guid TestUserId = Guid.NewGuid();
        #endregion

        #region Private Fields
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IUserRegistrationDependencies> _depsMock;
        private readonly Mock<ILogger<UserRegistrationService>> _loggerMock;
        #endregion

        #region Constructor
        public UserRegistrationServiceTest()
        {
            _userManagerMock = CreateUserManagerMock();
            _depsMock = new Mock<IUserRegistrationDependencies>();
            _loggerMock = new Mock<ILogger<UserRegistrationService>>();
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

        private UserRegistrationService CreateUserRegistrationService()
        {
            return new UserRegistrationService(
                _userManagerMock.Object,
                _depsMock.Object,
                _loggerMock.Object);
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

        private RegisterRequest CreateSellerRegisterRequest()
        {
            return new RegisterRequest
            {
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                Password = TestPassword,
                ConfirmPassword = TestPassword,
                Role = Role.Seller,
                Gender = Gender.Female,
                Age = 30
            };
        }

        private ApplicationUser CreateTestUser()
        {
            return new ApplicationUser
            {
                Id = TestUserId,
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                UserName = TestEmail,
                EmailConfirmed = false
            };
        }

        private List<string> CreateTestRoles()
        {
            return new List<string> { "Customer", "User" };
        }

        private UserResponse CreateTestUserResponse()
        {
            return new UserResponse
            {
                UserId = TestUserId,
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                Role = Role.Customer,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
        }

        private void SetupSuccessfulRegistration()
        {
            var user = CreateTestUser();
            var roles = CreateTestRoles();
            var userResponse = CreateTestUserResponse();
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.GetUserRolesAsync(user))
                .ReturnsAsync(roles);

            _depsMock
                .Setup(d => d.TokenHelper.GenerateTokens(user, roles))
                .Returns((TestAccessToken, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration));

            _depsMock
                .Setup(d => d.RefreshTokenService.StoreRefreshTokenAsync(user.Id, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.EmailHelper.SendConfirmationEmailAsync(user))
                .ReturnsAsync(true);
        }
        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidCustomerRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            SetupSuccessfulRegistration();
            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("User registered successfully as Customer", result.Message);
            Assert.Equal(TestAccessToken, result.AccessToken);
            Assert.Equal(TestRefreshToken, result.RefreshToken);
            Assert.NotNull(result.User);
        }

        [Fact]
        public async Task RegisterAsync_WithValidSellerRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = CreateSellerRegisterRequest();
            var user = CreateTestUser();
            var roles = new List<string> { "Seller", "User" };
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Seller"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Seller"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.GetUserRolesAsync(user))
                .ReturnsAsync(roles);

            _depsMock
                .Setup(d => d.TokenHelper.GenerateTokens(user, roles))
                .Returns((TestAccessToken, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration));

            _depsMock
                .Setup(d => d.RefreshTokenService.StoreRefreshTokenAsync(user.Id, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.EmailHelper.SendConfirmationEmailAsync(user))
                .ReturnsAsync(true);

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("User registered successfully as Seller", result.Message);
            Assert.Equal(TestAccessToken, result.AccessToken);
            Assert.Equal(TestRefreshToken, result.RefreshToken);
            Assert.NotNull(result.User);
        }

        [Fact]
        public async Task RegisterAsync_WithInvalidRole_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.Role = (Role)999; // Invalid role

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
            Assert.Contains("Invalid role", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUser_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var existingUser = CreateTestUser();

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync(existingUser);

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
            Assert.Contains("User with this email already exists", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenUserCreationFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync((ApplicationUser?)null);

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
            Assert.Contains("Failed to create user account", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenRoleCreationFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var user = CreateTestUser();

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(false);

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
            Assert.Contains("Failed to create role 'Customer'", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenRoleAssignmentFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var user = CreateTestUser();

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Customer"))
                .ReturnsAsync(false);

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
            Assert.Contains("Failed to assign role 'Customer' to user", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenTokenGenerationFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var user = CreateTestUser();
            var roles = CreateTestRoles();

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.GetUserRolesAsync(user))
                .ReturnsAsync(roles);

            _depsMock
                .Setup(d => d.TokenHelper.GenerateTokens(user, roles))
                .Throws(new Exception("Token generation failed"));

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenRefreshTokenStorageFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var user = CreateTestUser();
            var roles = CreateTestRoles();
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.GetUserRolesAsync(user))
                .ReturnsAsync(roles);

            _depsMock
                .Setup(d => d.TokenHelper.GenerateTokens(user, roles))
                .Returns((TestAccessToken, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration));

            _depsMock
                .Setup(d => d.RefreshTokenService.StoreRefreshTokenAsync(user.Id, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration))
                .ThrowsAsync(new Exception("Refresh token storage failed"));

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenEmailSendingFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            var user = CreateTestUser();
            var roles = CreateTestRoles();
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync((ApplicationUser?)null);

            _depsMock
                .Setup(d => d.UserCreationService.CreateUserAsync(request))
                .ReturnsAsync(user);

            _depsMock
                .Setup(d => d.RoleManagementHelper.EnsureRoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.AssignRoleToUserAsync(user, "Customer"))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.RoleManagementHelper.GetUserRolesAsync(user))
                .ReturnsAsync(roles);

            _depsMock
                .Setup(d => d.TokenHelper.GenerateTokens(user, roles))
                .Returns((TestAccessToken, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration));

            _depsMock
                .Setup(d => d.RefreshTokenService.StoreRefreshTokenAsync(user.Id, TestRefreshToken, accessTokenExpiration, refreshTokenExpiration))
                .ReturnsAsync(true);

            _depsMock
                .Setup(d => d.EmailHelper.SendConfirmationEmailAsync(user))
                .ThrowsAsync(new Exception("Email sending failed"));

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithInvalidEmail_ReturnsFailureResponse(string invalidEmail)
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.Email = invalidEmail;

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithInvalidFirstName_ReturnsFailureResponse(string invalidFirstName)
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.FirstName = invalidFirstName;

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithInvalidLastName_ReturnsFailureResponse(string invalidLastName)
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.LastName = invalidLastName;

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")] // Too short
        public async Task RegisterAsync_WithInvalidPassword_ReturnsFailureResponse(string invalidPassword)
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.Password = invalidPassword;
            request.ConfirmPassword = invalidPassword;

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithPasswordMismatch_ReturnsFailureResponse()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.ConfirmPassword = "DifferentPassword123!";

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Registration failed", result.Message);
        }

        #endregion

        #region Service Integration Tests

        [Fact]
        public void UserRegistrationService_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var userRegistrationService = CreateUserRegistrationService();

            // Assert
            Assert.NotNull(userRegistrationService);
        }

        [Fact]
        public void UserRegistrationService_ImplementsIUserRegistrationService()
        {
            // Arrange & Act
            var userRegistrationService = CreateUserRegistrationService();

            // Assert
            Assert.IsAssignableFrom<IUserRegistrationService>(userRegistrationService);
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task RegisterAsync_WithValidRequest_LogsInformationMessages()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            SetupSuccessfulRegistration();
            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Verify logging calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User registration started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User registration completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenRegistrationFails_LogsError()
        {
            // Arrange
            var request = CreateValidRegisterRequest();
            request.Role = (Role)999; // Invalid role

            var userRegistrationService = CreateUserRegistrationService();

            // Act
            var result = await userRegistrationService.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            
            // Verify error logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registration failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
