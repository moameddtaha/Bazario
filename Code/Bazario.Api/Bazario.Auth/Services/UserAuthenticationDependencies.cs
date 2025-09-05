using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Auth.Helpers;
using Bazario.Auth.ServiceContracts;

namespace Bazario.Auth.Services
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

        public UserAuthenticationDependencies(
            ITokenHelper tokenHelper,
            IRoleManagementHelper roleManagementHelper,
            ICustomerRepository customerRepository,
            ISellerRepository sellerRepository,
            IAdminRepository adminRepository)
        {
            TokenHelper = tokenHelper;
            RoleManagementHelper = roleManagementHelper;
            CustomerRepository = customerRepository;
            SellerRepository = sellerRepository;
            AdminRepository = adminRepository;
        }
    }
}
