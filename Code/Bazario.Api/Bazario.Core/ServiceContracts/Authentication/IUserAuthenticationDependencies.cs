using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Core.Helpers.Authentication;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Aggregates dependencies needed for user authentication operations
    /// </summary>
    public interface IUserAuthenticationDependencies
    {
        /// <summary>
        /// Helper for token generation and management
        /// </summary>
        ITokenHelper TokenHelper { get; }
        
        /// <summary>
        /// Helper for role management operations
        /// </summary>
        IRoleManagementHelper RoleManagementHelper { get; }
        
        /// <summary>
        /// Repository for customer-specific operations
        /// </summary>
        ICustomerRepository CustomerRepository { get; }
        
        /// <summary>
        /// Repository for seller-specific operations
        /// </summary>
        ISellerRepository SellerRepository { get; }
        
        /// <summary>
        /// Repository for admin-specific operations
        /// </summary>
        IAdminRepository AdminRepository { get; }
        
        /// <summary>
        /// Service for refresh token operations
        /// </summary>
        IRefreshTokenService RefreshTokenService { get; }
    }
}
