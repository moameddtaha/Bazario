using Bazario.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations
{
    public class StoreConfiguration : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> builder)
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
