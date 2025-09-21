using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Auth;
using Bazario.Core.ServiceContracts.Auth;

namespace Bazario.Core.Services.Auth
{
    /// <summary>
    /// Implementation of user authentication dependencies aggregator
    /// </summary>
    public class UserAuthenticationDependencies : IUserAuthenticationDependencies
    {
        public ITokenHelper TokenHelper { get; }
        public IRoleManagementHelper RoleManagementHelper { get; }
        public ICustomerRepository CustomerRepository { get; }
        public ISellerRepository SellerRepository { get; }
        public IAdminRepository AdminRepository { get; }
        public IRefreshTokenService RefreshTokenService { get; }

        public UserAuthenticationDependencies(
            ITokenHelper tokenHelper,
            IRoleManagementHelper roleManagementHelper,
            ICustomerRepository customerRepository,
            ISellerRepository sellerRepository,
            IAdminRepository adminRepository,
            IRefreshTokenService refreshTokenService)
        {
            TokenHelper = tokenHelper;
            RoleManagementHelper = roleManagementHelper;
            CustomerRepository = customerRepository;
            SellerRepository = sellerRepository;
            AdminRepository = adminRepository;
            RefreshTokenService = refreshTokenService;
        }
    }
}
