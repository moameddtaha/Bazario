using Bazario.Core.Domain.Entities.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Store
{
    /// <summary>
    /// Entity Framework configuration for StoreShippingConfiguration
    /// </summary>
    public class StoreShippingConfigurationConfiguration : IEntityTypeConfiguration<StoreShippingConfiguration>
    {
        public void Configure(EntityTypeBuilder<StoreShippingConfiguration> builder)
        {
            // Primary key
            builder.HasKey(sc => sc.ConfigurationId);

            // Relationship with Store
            builder.HasOne(sc => sc.Store)
                .WithMany()
                .HasForeignKey(sc => sc.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(sc => sc.StoreId)
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            builder.HasIndex(sc => sc.ConfigurationId)
                .IsUnique();

            // Table name
            builder.ToTable("StoreShippingConfigurations");
            
            // EF automatically handles all property mappings and data types!
        }
    }
}
