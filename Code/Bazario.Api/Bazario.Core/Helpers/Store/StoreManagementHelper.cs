using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Auth;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Helper class for store management operations
    /// Contains common business logic for store permissions and validation
    /// </summary>
    public class StoreManagementHelper : IStoreManagementHelper
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IRoleManagementHelper _roleManagementHelper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StoreManagementHelper> _logger;

        public StoreManagementHelper(
            IStoreRepository storeRepository,
            IRoleManagementHelper roleManagementHelper,
            UserManager<ApplicationUser> userManager,
            ILogger<StoreManagementHelper> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _roleManagementHelper = roleManagementHelper ?? throw new ArgumentNullException(nameof(roleManagementHelper));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if a user has admin privileges
        /// </summary>
        public async Task<bool> IsUserAdminAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User not found for admin check: {UserId}", userId);
                    return false;
                }

                var isAdmin = await _roleManagementHelper.UserHasRoleAsync(user, "Admin");
                _logger.LogDebug("Admin check for user {UserId}: {IsAdmin}", userId, isAdmin);
                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check admin status for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Checks if a user can perform an action on a store (owner or admin)
        /// </summary>
        public async Task<bool> CanUserManageStoreAsync(Guid userId, Guid storeId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if user is admin first
                var isAdmin = await IsUserAdminAsync(userId, cancellationToken);
                if (isAdmin)
                {
                    _logger.LogDebug("User {UserId} has admin privileges for store {StoreId}", userId, storeId);
                    return true;
                }

                // Check if user owns the store
                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                if (store == null)
                {
                    _logger.LogWarning("Store not found for ownership check: {StoreId}", storeId);
                    return false;
                }

                var isOwner = store.SellerId == userId;
                _logger.LogDebug("Ownership check for user {UserId} and store {StoreId}: {IsOwner}", userId, storeId, isOwner);
                return isOwner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check store management permissions for user: {UserId}, store: {StoreId}", userId, storeId);
                return false;
            }
        }
    }
}
