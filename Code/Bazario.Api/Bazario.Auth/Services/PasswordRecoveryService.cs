using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Auth.Exceptions;
using Bazario.Auth.Helpers;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for password recovery operations
    /// </summary>
    public class PasswordRecoveryService : IPasswordRecoveryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailHelper _emailHelper;
        private readonly ILogger<PasswordRecoveryService> _logger;

        public PasswordRecoveryService(
            UserManager<ApplicationUser> userManager,
            IEmailHelper emailHelper,
            ILogger<PasswordRecoveryService> logger)
        {
            _userManager = userManager;
            _emailHelper = emailHelper;
            _logger = logger;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    throw new AuthException("User not found with this email address.", AuthException.ErrorCodes.UserNotFound);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return await _emailHelper.SendPasswordResetEmailAsync(user, token);
            }
            catch (AuthException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed: {Email}", email);
                throw new AuthException("Failed to process password reset request.", AuthException.ErrorCodes.ValidationError, ex);
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    throw new AuthException("User not found with this email address.", AuthException.ErrorCodes.UserNotFound);
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    throw new ValidationException("Password reset failed. Please check your token and try again.", "PasswordReset", errors);
                }
                
                return true;
            }
            catch (AuthException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (ValidationException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed: {Email}", request.Email);
                throw new AuthException("Failed to reset password.", AuthException.ErrorCodes.ValidationError, ex);
            }
        }
    }
}
