using Bazario.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations
{
    public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
    {
        public void Configure(EntityTypeBuilder<Discount> builder)
        {
            // Primary key
            builder.HasKey(e => e.DiscountId);

            // Store relationship (optional - null for global discounts)
            builder
                .HasOne(e => e.Store)
                .WithMany() // Store doesn't have a collection of discounts
                .HasForeignKey(e => e.ApplicableStoreId)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if store is deleted

            // Indexes for performance
            builder.HasIndex(e => e.Code).IsUnique(); // Unique discount codes
            builder.HasIndex(e => e.ApplicableStoreId);
            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.IsUsed);
            builder.HasIndex(e => new { e.ValidFrom, e.ValidTo }); // Date range queries
            builder.HasIndex(e => new { e.Code, e.IsActive, e.IsUsed }); // Common query combination

            // Column configurations
            builder.Property(e => e.Value)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            builder.Property(e => e.MinimumOrderAmount)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            // String length constraints
            builder.Property(e => e.Code)
                .HasMaxLength(50);

            builder.Property(e => e.Description)
                .HasMaxLength(500);

            // Default values
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.Property(e => e.IsUsed)
                .HasDefaultValue(false);

            builder.Property(e => e.MinimumOrderAmount)
                .HasDefaultValue(0);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Soft delete filter - only show active discounts
            builder.HasQueryFilter(d => d.IsActive);
        }
    }
}
