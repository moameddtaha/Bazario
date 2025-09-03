using Bazario.Core.Domain.IdentityEntities;
using Bazario.Auth.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Bazario.Auth.Helpers;
using Bazario.Auth.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for user creation operations that require dependencies
    /// </summary>
    public class UserCreationService : IUserCreationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserCreationService> _logger;

        public UserCreationService(
            UserManager<ApplicationUser> userManager,
            ILogger<UserCreationService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Creates and saves a user with password
        /// </summary>
        public async Task<ApplicationUser?> CreateUserAsync(RegisterRequest request)
        {
            var user = UserCreationHelper.CreateUserFromRequest(request);
            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("User creation failed: {Email} - {Errors}", request.Email, string.Join(", ", errors));
                return null;
            }

            return user;
        }
    }
}
