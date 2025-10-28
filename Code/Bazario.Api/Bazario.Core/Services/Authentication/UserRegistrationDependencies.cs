using Bazario.Core.Helpers.Authentication;
using Bazario.Core.ServiceContracts.Authentication;
using Bazario.Core.Helpers.Infrastructure;

namespace Bazario.Core.Services.Authentication
{
    /// <summary>
    /// Implementation of user registration dependencies aggregator
    /// </summary>
    public class UserRegistrationDependencies : IUserRegistrationDependencies
    {
        public IUserCreationService UserCreationService { get; }
        public IRoleManagementService RoleManagementService { get; }
        public ITokenHelper TokenHelper { get; }
        public IEmailHelper EmailHelper { get; }
        public IRefreshTokenService RefreshTokenService { get; }
        public UserRegistrationDependencies(
            IUserCreationService userCreationService,
            IRoleManagementService roleManagementService,
            ITokenHelper tokenHelper,
            IEmailHelper emailHelper,
            IRefreshTokenService refreshTokenService)
        {
            UserCreationService = userCreationService;
            RoleManagementService = roleManagementService;
            TokenHelper = tokenHelper;
            EmailHelper = emailHelper;
            RefreshTokenService = refreshTokenService;
        }
    }
}
