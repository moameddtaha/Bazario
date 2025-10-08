using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.ServiceContracts.Authentication;

namespace Bazario.Core.Services.Authentication
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
