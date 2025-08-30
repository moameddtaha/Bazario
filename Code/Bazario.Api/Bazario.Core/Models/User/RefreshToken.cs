using System.ComponentModel.DataAnnotations;

namespace Bazario.Core.Models.User
{
    public class RefreshToken
    {
        public string Id { get; set; } = string.Empty;
        
        public string UserId { get; set; } = string.Empty;
        
        public string Token { get; set; } = string.Empty;
        
        public DateTime AccessTokenExpiresAt { get; set; }
        
        public DateTime RefreshTokenExpiresAt { get; set; }
        
        public bool IsRevoked { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        public string? RevokedBy { get; set; }
        
        public string? RevocationReason { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastUsedAt { get; set; }
        
        public string? UserAgent { get; set; }
        
        public string? IpAddress { get; set; }
    }
}
