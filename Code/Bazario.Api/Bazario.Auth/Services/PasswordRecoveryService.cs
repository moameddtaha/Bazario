using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;
using Bazario.Email.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Service for password recovery operations
    /// </summary>
    public class PasswordRecoveryService : IPasswordRecoveryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordRecoveryService> _logger;

        public PasswordRecoveryService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<PasswordRecoveryService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return false;
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return await SendPasswordResetEmailAsync(user, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password process for email: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return false;
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", request.Email);
                return false;
            }
        }

        private async Task<bool> SendPasswordResetEmailAsync(ApplicationUser user, string token)
        {
            try
            {
                // Check if user has a valid email
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("Cannot send password reset email: User {UserId} has no valid email address", user.Id);
                    return false;
                }

                var resetUrl = _configuration["AppSettings:PasswordResetUrl"] ?? "https://localhost:5001/reset-password";
                var userName = GetUserName(user);

                return await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    userName,
                    token,
                    resetUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to user {UserId} at {Email}", user.Id, user.Email ?? "unknown");
                return false;
            }
        }

        private static string GetUserName(ApplicationUser user)
        {
            // Try to get a meaningful name, fallback to email, then to generic "User"
            if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                return user.FirstName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }
            
            return "User";
        }
    }
}
