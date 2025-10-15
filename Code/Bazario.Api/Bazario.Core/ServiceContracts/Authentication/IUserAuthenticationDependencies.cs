using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Authentication;

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

        /// <summary>
        /// Helper for role management operations
        /// </summary>
        IRoleManagementHelper RoleManagementHelper { get; }

        /// <summary>
        /// Unit of Work for coordinated repository access (Customers, Sellers, Admins, etc.)
        /// </summary>
        IUnitOfWork UnitOfWork { get; }

        /// <summary>
        /// Service for refresh token operations
        /// </summary>
        IRefreshTokenService RefreshTokenService { get; }
    }
}
