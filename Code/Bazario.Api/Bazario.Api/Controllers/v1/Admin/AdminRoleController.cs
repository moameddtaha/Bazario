using Asp.Versioning;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.ServiceContracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Admin
{
    /// <summary>
    /// Handles administrative role management operations
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/roles")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoleController : ControllerBase
    {
        private readonly IRoleManagementService _roleManagementService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminRoleController> _logger;

        public AdminRoleController(
            IRoleManagementService roleManagementService,
            IRefreshTokenService refreshTokenService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminRoleController> logger)
        {
            _roleManagementService = roleManagementService;
            _refreshTokenService = refreshTokenService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Ensures a role exists in the system
        /// </summary>
        /// <param name="roleName">Name of the role (Admin, Customer, Seller)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Role ensured successfully</response>
        /// <response code="400">Invalid role name</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">Not authorized (Admin only)</response>
        [HttpPost("ensure")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> EnsureRoleExists(
            [FromBody] string roleName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} ensuring role exists: {RoleName}", adminId, roleName);

                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return BadRequest(new { message = "Role name is required" });
                }

                await _roleManagementService.EnsureRoleExistsAsync(roleName);

                _logger.LogInformation("Role ensured successfully: {RoleName}", roleName);
                return Ok(new { message = $"Role '{roleName}' ensured successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid role name provided");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring role exists: {RoleName}", roleName);
                return StatusCode(500, new { message = "An error occurred while ensuring the role" });
            }
        }

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        /// <param name="request">Role assignment details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Role assigned successfully</response>
        /// <response code="400">Invalid request or user not found</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">Not authorized (Admin only)</response>
        [HttpPost("assign")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> AssignRole(
            [FromBody] AssignRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} assigning role {RoleName} to user {UserId}",
                    adminId, request.RoleName, request.UserId);

                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", request.UserId);
                    return BadRequest(new { message = $"User with ID {request.UserId} not found" });
                }

                await _roleManagementService.AssignRoleToUserAsync(user, request.RoleName);

                _logger.LogInformation("Role {RoleName} assigned to user {UserId} successfully",
                    request.RoleName, request.UserId);
                return Ok(new { message = $"Role '{request.RoleName}' assigned to user successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid role assignment request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return StatusCode(500, new { message = "An error occurred while assigning the role" });
            }
        }

        /// <summary>
        /// Gets all roles assigned to a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of role names</returns>
        /// <response code="200">Roles retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">Not authorized (Admin only)</response>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<string>>> GetUserRoles(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} fetching roles for user {UserId}", adminId, userId);

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return NotFound(new { message = $"User with ID {userId} not found" });
                }

                var roles = await _roleManagementService.GetUserRolesAsync(user);

                _logger.LogInformation("Roles retrieved for user {UserId}: {RoleCount} roles", userId, roles.Count);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving user roles" });
            }
        }

        /// <summary>
        /// Revokes all refresh tokens for a specific user (force logout)
        /// </summary>
        /// <param name="request">Token revocation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">All tokens revoked successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">Not authorized (Admin only)</response>
        [HttpPost("revoke-all-tokens")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> RevokeAllUserTokens(
            [FromBody] RevokeAllTokensRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} revoking all tokens for user {UserId}",
                    adminId, request.UserId);

                await _refreshTokenService.RevokeAllUserTokensAsync(
                    request.UserId,
                    adminId.ToString(),
                    request.Reason);

                _logger.LogInformation("All tokens revoked for user {UserId} by admin {AdminId}",
                    request.UserId, adminId);
                return Ok(new { message = "All user tokens revoked successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid revoke all tokens request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all user tokens");
                return StatusCode(500, new { message = "An error occurred while revoking tokens" });
            }
        }

        /// <summary>
        /// Extracts the current admin user's ID from JWT claims
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }
    }

    /// <summary>
    /// Request model for assigning a role to a user
    /// </summary>
    public class AssignRoleRequest
    {
        /// <summary>
        /// User ID to assign the role to
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Role name to assign (Admin, Customer, Seller)
        /// </summary>
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for revoking all tokens for a user
    /// </summary>
    public class RevokeAllTokensRequest
    {
        /// <summary>
        /// User ID whose tokens should be revoked
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Reason for revoking tokens (optional)
        /// </summary>
        public string? Reason { get; set; }
    }
}
