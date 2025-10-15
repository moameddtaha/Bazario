using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.ServiceContracts.Authentication;

namespace Bazario.Core.Services.Authentication
{
    /// <summary>
    /// Implementation of user authentication dependencies aggregator
    /// Uses Unit of Work pattern for repository access
    /// </summary>
    public class UserAuthenticationDependencies : IUserAuthenticationDependencies
    {
        public ITokenHelper TokenHelper { get; }
        public IRoleManagementHelper RoleManagementHelper { get; }
        public IUnitOfWork UnitOfWork { get; }
        public IRefreshTokenService RefreshTokenService { get; }

        public UserAuthenticationDependencies(
            ITokenHelper tokenHelper,
            IRoleManagementHelper roleManagementHelper,
            IUnitOfWork unitOfWork,
            IRefreshTokenService refreshTokenService)
        {
            TokenHelper = tokenHelper;
            RoleManagementHelper = roleManagementHelper;
            UnitOfWork = unitOfWork;
            RefreshTokenService = refreshTokenService;
        }
    }
}
