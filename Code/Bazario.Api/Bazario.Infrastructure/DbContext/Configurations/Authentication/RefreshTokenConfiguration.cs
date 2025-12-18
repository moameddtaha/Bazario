using Bazario.Core.Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Authentication
{
    /// <summary>
    /// Entity Framework configuration for RefreshToken entity
    /// Adds critical database indexes for performance and security
    /// </summary>
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // CRITICAL: Unique index on Token column for fast lookups and uniqueness guarantee
            // Without this index, token validation performs full table scans (O(n) complexity)
            // With this index, lookups are O(log n) using B-tree index
            builder.HasIndex(rt => rt.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            // Composite index for efficient cleanup operations
            // Used by CleanupExpiredTokensAsync to find expired non-revoked tokens
            // Allows index-only scan without accessing table data
            builder.HasIndex(rt => new { rt.RefreshTokenExpiresAt, rt.IsRevoked })
                .HasDatabaseName("IX_RefreshTokens_Expiration_Revocation");

            // Composite index for user token queries
            // Used by RevokeAllUserTokensAsync and GetUserTokensAsync
            // Enables efficient filtering by user and revocation status
            builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked })
                .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked");
        }
    }
}
