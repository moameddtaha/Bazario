using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.ServiceContracts.Authentication;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Aggregates dependencies needed for user authentication operations
    /// Uses Unit of Work pattern for coordinated repository access
    /// </summary>
    public interface IUserAuthenticationDependencies
    {
        /// <summary>
        /// Helper for token generation and management
        /// </summary>
        ITokenHelper TokenHelper { get; }
        /// Service for role management operations
        IRoleManagementService RoleManagementService { get; }
        /// Unit of Work for coordinated repository access (Customers, Sellers, Admins, etc.)
        IUnitOfWork UnitOfWork { get; }
        /// Service for refresh token operations
        IRefreshTokenService RefreshTokenService { get; }
    }
}
