using System.ComponentModel.DataAnnotations;
using Bazario.Core.Enums;

namespace Bazario.Auth.DTO
{
    /// <summary>
    /// User registration request
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; } = Role.Customer; // Default to Customer, but can be Seller or Customer

        public Gender? Gender { get; set; }

        [Range(13, 120)]
        public int? Age { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
