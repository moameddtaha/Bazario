using Bazario.Core.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Inventory
{
    /// <summary>
    /// Entity Framework configuration for InventoryAlertPreferences
    /// </summary>
    public class InventoryAlertPreferencesConfiguration : IEntityTypeConfiguration<InventoryAlertPreferences>
    {
        public void Configure(EntityTypeBuilder<InventoryAlertPreferences> builder)
        {
            // Indexes for performance
            builder.HasIndex(p => p.AlertEmail);
            builder.HasIndex(p => p.CreatedAt);
            builder.HasIndex(p => p.UpdatedAt);

            // Table name
            builder.ToTable("InventoryAlertPreferences");
        }
    }
}
