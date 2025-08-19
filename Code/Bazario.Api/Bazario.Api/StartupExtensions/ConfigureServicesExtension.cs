using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Bazario.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Api.StartupExtensions
{
    public static class ConfigureServicesExtension
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add your service configurations here
            // Example: services.AddScoped<IMyService, MyService>();

            // Add services to the container.

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            // Add Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Register Repositories
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ISellerRepository, SellerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Register Services

            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
    }
}
