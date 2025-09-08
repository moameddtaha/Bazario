using Bazario.Auth.Helpers;
using Bazario.Auth.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Implementation of user registration dependencies aggregator
    /// </summary>
    public class UserRegistrationDependencies : IUserRegistrationDependencies
    {
        public IUserCreationService UserCreationService { get; }
        public IRoleManagementHelper RoleManagementHelper { get; }
        public ITokenHelper TokenHelper { get; }
        public IEmailHelper EmailHelper { get; }
        public IRefreshTokenService RefreshTokenService { get; }

        public UserRegistrationDependencies(
            IUserCreationService userCreationService,
            IRoleManagementHelper roleManagementHelper,
            ITokenHelper tokenHelper,
            IEmailHelper emailHelper,
            IRefreshTokenService refreshTokenService)
        {
            UserCreationService = userCreationService;
            RoleManagementHelper = roleManagementHelper;
            TokenHelper = tokenHelper;
            EmailHelper = emailHelper;
            RefreshTokenService = refreshTokenService;
        }
    }
}
