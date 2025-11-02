using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Authentication;
using Bazario.Core.Domain.Entities.Catalog;
using InventoryAlertPreferencesEntity = Bazario.Core.Domain.Entities.Inventory.InventoryAlertPreferences;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.Entities.Order;
using Bazario.Core.Domain.Entities.Review;
using Bazario.Core.Domain.Entities.Store;
using Bazario.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockReservationEntity = Bazario.Core.Domain.Entities.Inventory.StockReservation;

namespace Bazario.Infrastructure.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Store> Stores { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<StoreShippingConfiguration> StoreShippingConfigurations { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<StoreGovernorateSupport> StoreGovernorateSupports { get; set; }
        public DbSet<StockReservationEntity> StockReservations { get; set; }
        public DbSet<InventoryAlertPreferencesEntity> InventoryAlertPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
