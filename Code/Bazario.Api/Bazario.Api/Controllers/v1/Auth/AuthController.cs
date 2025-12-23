using Asp.Versioning;
using Bazario.Core.DTO.Authentication;
using Bazario.Core.ServiceContracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Auth
{
    /// <summary>
    /// Handles public authentication operations including registration, login, and password recovery
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IPasswordRecoveryService passwordRecoveryService,
            IRefreshTokenService refreshTokenService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _passwordRecoveryService = passwordRecoveryService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user (Customer or Seller)
        /// </summary>
        /// <param name="request">User registration details including email, password, role, and profile information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with access token and refresh token</returns>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid request data or validation failure</response>
        /// <response code="409">User already exists</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Registration attempt for email: {Email} with role: {Role}",
                    request.Email, request.Role);

                var response = await _authService.RegisterAsync(request);

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Message}", request.Email, response.Message);
                    return BadRequest(new { message = response.Message, errors = response.Errors });
                }

                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Authenticates a user and returns access and refresh tokens
        /// </summary>
        /// <param name="request">Login credentials (email and password)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with tokens</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid credentials</response>
        /// <response code="401">Authentication failed</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                var response = await _authService.LoginAsync(request);

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, response.Message);
                    return Unauthorized(new { message = response.Message, errors = response.Errors });
                }

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Refreshes an expired access token using a valid refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New access token and refresh token</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="400">Invalid or expired refresh token</response>
        /// <response code="401">Refresh token validation failed</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> RefreshToken(
            [FromBody] string refreshToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                var response = await _refreshTokenService.RefreshTokenAsync(refreshToken);

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("Token refresh failed: {Message}", response.Message);
                    return Unauthorized(new { message = response.Message });
                }

                _logger.LogInformation("Token refreshed successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Initiates password recovery by sending a reset link to the user's email
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Password reset email sent</response>
        /// <response code="404">User not found</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ForgotPassword(
            [FromBody] string email,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Forgot password request for email: {Email}", email);

                await _passwordRecoveryService.ForgotPasswordAsync(email);

                _logger.LogInformation("Password reset email sent to: {Email}", email);
                return Ok(new { message = "Password reset instructions have been sent to your email" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found for password reset: {Email}", email);
                // Return success even if user not found (security best practice)
                return Ok(new { message = "If an account exists with that email, password reset instructions have been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Resets user password using a valid reset token
        /// </summary>
        /// <param name="request">Password reset details including token and new password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Password reset successfully</response>
        /// <response code="400">Invalid or expired reset token</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

                await _passwordRecoveryService.ResetPasswordAsync(request);

                _logger.LogInformation("Password reset successfully for: {Email}", request.Email);
                return Ok(new { message = "Password has been reset successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid password reset attempt for {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while resetting your password" });
            }
        }
    }
}
