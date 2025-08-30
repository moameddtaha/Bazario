using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.DTO.Auth
{
    /// <summary>
    /// User login request
    /// </summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
