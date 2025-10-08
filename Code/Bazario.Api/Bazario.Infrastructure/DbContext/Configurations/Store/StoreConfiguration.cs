using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Store
{
    public class StoreConfiguration : IEntityTypeConfiguration<StoreEntity>
    {
        public void Configure(EntityTypeBuilder<StoreEntity> builder)
        {
            builder
                .HasMany(e => e.Products) // Store has many Products
                .WithOne(e => e.Store) // Product has one Store
                .HasForeignKey(e => e.StoreId) // The foreign key is StoreId
                .OnDelete(DeleteBehavior.Restrict);

            // Add soft delete filter
            builder.HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
