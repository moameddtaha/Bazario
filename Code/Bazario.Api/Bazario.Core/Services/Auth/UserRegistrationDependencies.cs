using Bazario.Core.Helpers.Auth;
using Bazario.Core.Helpers.Email;
using Bazario.Core.ServiceContracts.Auth;

namespace Bazario.Core.Services.Auth
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
