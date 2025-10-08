using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.DTO.Authentication;

namespace Bazario.Core.ServiceContracts.Authentication
{
    /// <summary>
    /// Interface for user creation operations that require dependencies
    /// </summary>
    public interface IUserCreationService
    {
        /// <summary>
        /// Creates and saves a user with password
        /// </summary>
        Task<ApplicationUser?> CreateUserAsync(RegisterRequest request);
    }
}
