using Bazario.Core.Domain.IdentityEntities;
using Bazario.Auth.DTO;

namespace Bazario.Auth.ServiceContracts
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
