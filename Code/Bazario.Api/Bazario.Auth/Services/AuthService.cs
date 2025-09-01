using Bazario.Auth.DTO;
using Bazario.Auth.ServiceContracts;

namespace Bazario.Auth.Services
{
    /// <summary>
    /// Main authentication service that coordinates other focused services
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly IUserAuthenticationService _userAuthenticationService;
        private readonly IUserManagementService _userManagementService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public AuthService(
            IUserRegistrationService userRegistrationService,
            IUserAuthenticationService userAuthenticationService,
            IUserManagementService userManagementService,
            IPasswordRecoveryService passwordRecoveryService)
        {
            _userRegistrationService = userRegistrationService;
            _userAuthenticationService = userAuthenticationService;
            _userManagementService = userManagementService;
            _passwordRecoveryService = passwordRecoveryService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            return await _userRegistrationService.RegisterAsync(request);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            return await _userAuthenticationService.LoginAsync(request);
        }

        public async Task<UserResult> GetCurrentUserAsync(Guid userId)
        {
            return await _userManagementService.GetCurrentUserAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            return await _userManagementService.ChangePasswordAsync(userId, request);
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            return await _passwordRecoveryService.ForgotPasswordAsync(email);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            return await _passwordRecoveryService.ResetPasswordAsync(request);
        }
    }
}
