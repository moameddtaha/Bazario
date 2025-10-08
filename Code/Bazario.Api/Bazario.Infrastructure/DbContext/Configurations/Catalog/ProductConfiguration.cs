using Bazario.Core.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Catalog
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder
                .HasMany(e => e.Reviews) // Product has many Reviews
                .WithOne(e => e.Product) // Review has one Product
                .HasForeignKey(e => e.ProductId) // The foreign key is ProductId
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasMany(e => e.OrderItems) // Product has many OrderItems
                .WithOne(e => e.Product) // OrderItem has one Product
                .HasForeignKey(e => e.ProductId) // The foreign key is ProductId
                .OnDelete(DeleteBehavior.Restrict);

            // Add soft delete filter
            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
