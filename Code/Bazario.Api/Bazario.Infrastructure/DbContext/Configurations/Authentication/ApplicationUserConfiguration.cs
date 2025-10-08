using Bazario.Core.Domain.IdentityEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Authentication
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder
                .HasMany(e => e.Stores) // ApplicationUser has many Stores
                .WithOne(e => e.Seller) // Store has one Seller
                .HasForeignKey(e => e.SellerId) // The foreign key is SellerId
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(e => e.Reviews) // ApplicationUser has many Reviews
                .WithOne(e => e.Customer) // Review has one Customer
                .HasForeignKey(e => e.CustomerId) // The foreign key is CustomerId
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(e => e.Orders) // ApplicationUser has many Orders
                .WithOne(e => e.Customer) // Order has one Customer
                .HasForeignKey(e => e.CustomerId) // The foreign key is CustomerId
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
