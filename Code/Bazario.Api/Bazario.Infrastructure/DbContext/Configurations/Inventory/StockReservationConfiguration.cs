using Bazario.Core.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bazario.Infrastructure.DbContext.Configurations.Inventory
{
    public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
    {
        public void Configure(EntityTypeBuilder<StockReservation> builder)
        {
            // StockReservation-Product relationship
            builder
                .HasOne(r => r.Product)
                .WithMany() // Product doesn't need collection of reservations
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // StockReservation-Customer relationship
            builder
                .HasOne(r => r.Customer)
                .WithMany() // Customer doesn't need collection of reservations
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // StockReservation-Order relationship (optional)
            builder
                .HasOne(r => r.Order)
                .WithMany() // Order doesn't need collection of reservations
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // OrderId is nullable

            // Add soft delete filter
            builder.HasQueryFilter(r => !r.IsDeleted);

            // Indexes for performance optimization
            builder.HasIndex(r => r.ReservationId); // Index for grouping reservation items
            builder.HasIndex(r => r.ProductId);
            builder.HasIndex(r => r.CustomerId);
            builder.HasIndex(r => r.OrderId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => r.ExpiresAt);
            builder.HasIndex(r => r.CreatedAt);
            builder.HasIndex(r => r.IsDeleted); // Index for soft delete queries

            // Composite indexes for common query patterns
            builder.HasIndex(r => new { r.ReservationId, r.Status }); // Get all items in a reservation by status
            builder.HasIndex(r => new { r.ProductId, r.Status }); // Get active reservations by product
            builder.HasIndex(r => new { r.Status, r.ExpiresAt }); // Find expired pending reservations
            builder.HasIndex(r => new { r.CustomerId, r.Status }); // Get customer active reservations
        }
    }
}
