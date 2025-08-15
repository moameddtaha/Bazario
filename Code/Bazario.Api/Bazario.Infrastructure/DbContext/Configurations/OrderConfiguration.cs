using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder
                .HasMany(e => e.OrderItems) // Order has many OrderItems
                .WithOne(e => e.Order) // OrderItem has one Order
                .HasForeignKey(e => e.OrderId) // The foreign key is OrderId
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
