using Bazario.Core.Helpers.Authentication;
using Bazario.Core.ServiceContracts.Authentication;
using Bazario.Core.Helpers.Infrastructure;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Aggregates dependencies needed for user registration operations
    /// </summary>
    public interface IUserRegistrationDependencies
    {
        /// <summary>
        /// Service for user creation operations
        /// </summary>
        IUserCreationService UserCreationService { get; }
        
        /// Service for role management operations
        IRoleManagementService RoleManagementService { get; }
        /// Helper for token generation and management
        ITokenHelper TokenHelper { get; }
        /// Helper for email operations
        IEmailHelper EmailHelper { get; }
        /// Service for refresh token operations
        IRefreshTokenService RefreshTokenService { get; }
    }
}
