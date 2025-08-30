using System;
using System.ComponentModel.DataAnnotations;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.Entities
{
    /// <summary>
    /// Refresh token entity for database storage
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [Display(Name = "Access Token Expires At")]
        public DateTime AccessTokenExpiresAt { get; set; }
        
        [Required]
        [Display(Name = "Refresh Token Expires At")]
        public DateTime RefreshTokenExpiresAt { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [Required]
        public bool IsRevoked { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        [StringLength(50)]
        public string? RevokedBy { get; set; }
        
        [StringLength(200)]
        public string? RevocationReason { get; set; }
        
        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }
}
