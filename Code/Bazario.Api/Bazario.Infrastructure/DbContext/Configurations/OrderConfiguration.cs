using Bazario.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Order-OrderItems relationship
            builder
                .HasMany(e => e.OrderItems) // Order has many OrderItems
                .WithOne(e => e.Order) // OrderItem has one Order
                .HasForeignKey(e => e.OrderId) // The foreign key is OrderId
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal column configurations are defined in the entity using [Column(TypeName = "decimal(18,2)")]

            // String length constraints are defined in the entity using [StringLength(50)]

            // Indexes for performance
            builder.HasIndex(e => e.CustomerId);
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.AppliedDiscountCodes);
            builder.HasIndex(e => e.Date);
        }
    }
}
