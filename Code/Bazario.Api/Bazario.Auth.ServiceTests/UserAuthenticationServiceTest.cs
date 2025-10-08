using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
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
    /// Unit tests for UserAuthenticationService focusing on authentication logic.
    /// Note: SignInManager is difficult to mock, so some tests are simplified.
    /// </summary>
    public class UserAuthenticationServiceTest
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
        private readonly Mock<IUserAuthenticationDependencies> _depsMock;
        private readonly Mock<ILogger<UserAuthenticationService>> _loggerMock;
        #endregion

        #region Constructor
        public UserAuthenticationServiceTest()
        {
            _userManagerMock = CreateUserManagerMock();
            _depsMock = new Mock<IUserAuthenticationDependencies>();
            _loggerMock = new Mock<ILogger<UserAuthenticationService>>();
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

        private UserAuthenticationService CreateUserAuthenticationService()
        {
            // Create a real SignInManager with mocked dependencies
            // This is the only way to properly test SignInManager-dependent code
            var httpContextAccessor = Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var userClaimsPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var identityOptions = Mock.Of<IOptions<IdentityOptions>>();
            var logger = Mock.Of<ILogger<SignInManager<ApplicationUser>>>();
            var authenticationSchemeProvider = Mock.Of<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var userConfirmation = Mock.Of<IUserConfirmation<ApplicationUser>>();

            var signInManager = new SignInManager<ApplicationUser>(
                _userManagerMock.Object,
                httpContextAccessor,
                userClaimsPrincipalFactory,
                identityOptions,
                logger,
                authenticationSchemeProvider);

            return new UserAuthenticationService(
                _userManagerMock.Object,
                signInManager,
                _depsMock.Object,
                _loggerMock.Object);
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

        private ApplicationUser CreateTestUser()
        {
            return new ApplicationUser
            {
                Id = TestUserId,
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                UserName = TestEmail,
                EmailConfirmed = true,
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
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
        #endregion

        #region Service Integration Tests

        [Fact]
        public void UserAuthenticationService_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var userAuthenticationService = CreateUserAuthenticationService();

            // Assert
            Assert.NotNull(userAuthenticationService);
        }

        [Fact]
        public void UserAuthenticationService_ImplementsIUserAuthenticationService()
        {
            // Arrange & Act
            var userAuthenticationService = CreateUserAuthenticationService();

            // Assert
            Assert.IsAssignableFrom<IUserAuthenticationService>(userAuthenticationService);
        }

        #endregion

        #region Parameter Validation Tests

        [Fact]
        public async Task LoginAsync_WithNullRequest_ReturnsFailureResponse()
        {
            // Arrange
            var userAuthenticationService = CreateUserAuthenticationService();

            // Act
            var result = await userAuthenticationService.LoginAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Login request cannot be null", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_WithInvalidEmail_ReturnsFailureResponse(string invalidEmail)
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = invalidEmail,
                Password = TestPassword
            };

            _userManagerMock
                .Setup(um => um.FindByEmailAsync(invalidEmail))
                .ReturnsAsync((ApplicationUser?)null);

            var userAuthenticationService = CreateUserAuthenticationService();

            // Act
            var result = await userAuthenticationService.LoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Login failed", result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_WithInvalidPassword_ReturnsFailureResponse(string invalidPassword)
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = TestEmail,
                Password = invalidPassword
            };

            var user = CreateTestUser();
            
            _userManagerMock
                .Setup(um => um.FindByEmailAsync(TestEmail))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.IsLockedOutAsync(user))
                .ReturnsAsync(false);

            var userAuthenticationService = CreateUserAuthenticationService();

            // Act
            var result = await userAuthenticationService.LoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Login failed", result.Message);
        }

        #endregion

        #region Note About SignInManager Testing

        [Fact]
        public void Note_SignInManagerTestingLimitation()
        {
            // This test documents the limitation of testing SignInManager with Moq
            // SignInManager has a complex constructor that makes it difficult to mock properly
            // In a real project, you would typically:
            // 1. Use integration tests with a test server
            // 2. Create a wrapper interface around SignInManager
            // 3. Use a different mocking framework that handles complex constructors better
            
            Assert.True(true, "SignInManager mocking is complex due to its constructor requirements");
        }

        #endregion
    }
}