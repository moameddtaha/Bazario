using Bazario.Core.Helpers.Auth;
using Bazario.Core.Helpers.Email;

namespace Bazario.Core.ServiceContracts.Auth
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
        
        /// <summary>
        /// Helper for role management operations
        /// </summary>
        IRoleManagementHelper RoleManagementHelper { get; }
        
        /// <summary>
        /// Helper for token generation and management
        /// </summary>
        ITokenHelper TokenHelper { get; }
        
        /// <summary>
        /// Helper for email operations
        /// </summary>
        IEmailHelper EmailHelper { get; }
        
        /// <summary>
        /// Service for refresh token operations
        /// </summary>
        IRefreshTokenService RefreshTokenService { get; }
    }
}
