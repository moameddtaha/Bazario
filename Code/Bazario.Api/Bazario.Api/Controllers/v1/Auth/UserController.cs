using Asp.Versioning;
using Bazario.Core.DTO.Authentication;
using Bazario.Core.ServiceContracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Auth
{
    /// <summary>
    /// Handles authenticated user operations including profile management and token revocation
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [ApiController]
    [Authorize(Roles = "Customer,Seller,Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserManagementService userManagementService,
            IRefreshTokenService refreshTokenService,
            ILogger<UserController> logger)
        {
            _userManagementService = userManagementService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current authenticated user's profile information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User profile details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResult>> GetProfile(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Fetching profile for user: {UserId}", userId);

                var user = await _userManagementService.GetCurrentUserAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { message = "An error occurred while retrieving your profile" });
            }
        }

        /// <summary>
        /// Changes the current authenticated user's password
        /// </summary>
        /// <param name="request">Password change details including current and new password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid request or current password incorrect</response>
        /// <response code="401">User not authenticated</response>
        [HttpPut("change-password")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Password change attempt for user: {UserId}", userId);

                await _userManagementService.ChangePasswordAsync(userId, request);

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized password change attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid password change request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "An error occurred while changing your password" });
            }
        }

        /// <summary>
        /// Revokes the current user's refresh token (logout)
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Token revoked successfully</response>
        /// <response code="400">Invalid token</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("revoke-token")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> RevokeToken(
            [FromBody] string refreshToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Token revocation request from user: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                await _refreshTokenService.RevokeTokenAsync(refreshToken);

                _logger.LogInformation("Token revoked successfully for user: {UserId}", userId);
                return Ok(new { message = "Token revoked successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid token revocation request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return StatusCode(500, new { message = "An error occurred while revoking the token" });
            }
        }

        /// <summary>
        /// Extracts the current user's ID from JWT claims
        /// </summary>
        /// <returns>User ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user ID cannot be extracted from token</exception>
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
}
