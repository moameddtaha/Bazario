using Bazario.Core.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Store
{
    public class StoreGovernorateSupportConfiguration : IEntityTypeConfiguration<StoreGovernorateSupport>
    {
        public void Configure(EntityTypeBuilder<StoreGovernorateSupport> builder)
        {
            // Composite unique index to prevent duplicate store-governorate combinations
            builder
                .HasIndex(s => new { s.StoreId, s.GovernorateId })
                .IsUnique();

            // Store relationship (cascade delete - when store is deleted, remove its governorate support records)
            builder
                .HasOne(s => s.Store)
                .WithMany()
                .HasForeignKey(s => s.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Governorate relationship (restrict delete - prevent governorate deletion if used by stores)
            builder
                .HasOne(s => s.Governorate)
                .WithMany()
                .HasForeignKey(s => s.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}