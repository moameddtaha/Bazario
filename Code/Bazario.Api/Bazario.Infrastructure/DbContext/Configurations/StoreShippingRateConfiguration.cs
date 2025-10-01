using Bazario.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations
{
    public class StoreShippingRateConfiguration : IEntityTypeConfiguration<StoreShippingRate>
    {
        public void Configure(EntityTypeBuilder<StoreShippingRate> builder)
        {
            // Primary key
            builder.HasKey(e => e.StoreShippingRateId);

            // Store relationship
            builder
                .HasOne(e => e.Store)
                .WithMany(e => e.StoreShippingRates)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(e => e.StoreId);
            builder.HasIndex(e => e.ShippingZone);
            builder.HasIndex(e => new { e.StoreId, e.ShippingZone }).IsUnique(); // One rate per store per zone

            // Column configurations
            builder.Property(e => e.ShippingCost)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            builder.Property(e => e.FreeShippingThreshold)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            // Soft delete filter - only show active shipping rates
            builder.HasQueryFilter(s => s.IsActive);
        }
    }
}
