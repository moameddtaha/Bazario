using Bazario.Infrastructure.DbContext;
using Bazario.Infrastructure.DbContext.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Bazario.Core.ServiceContracts.Store;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.ServiceContracts.Inventory;
using Bazario.Core.ServiceContracts.Location;
using Bazario.Core.Domain.IdentityEntities;

using Bazario.Core.Services.Store;
using Bazario.Core.Services.Order;
using Bazario.Core.Services.Inventory;
using Bazario.Core.Services.Location;
using Bazario.Core.Services.Auth;
using Bazario.Core.Helpers.Order;
using Bazario.Core.Helpers.Inventory;
using Bazario.Core.Helpers.Store;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Domain.RepositoryContracts.Authentication;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts.Review;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.Catalog.Product;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.Models.Infrastructure;
using Bazario.Core.ServiceContracts.Authentication;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Bazario.Core.ServiceContracts.Infrastructure;
using Bazario.Core.Services.Authentication;
using Bazario.Core.Services.Catalog.Product;
using Bazario.Core.Services.Catalog.Discount;
using Bazario.Core.Services.Infrastructure;
using Bazario.Infrastructure.Repositories.Authentication;
using Bazario.Infrastructure.Repositories.UserManagement;
using Bazario.Infrastructure.Repositories.Catalog;
using Bazario.Infrastructure.Repositories.Review;
using Bazario.Infrastructure.Repositories.Location;
using Bazario.Infrastructure.Repositories.Order;
using Bazario.Infrastructure.Repositories.Store;
using Bazario.Infrastructure.Repositories;

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

            // Configure Email Settings
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // Add Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Register Unit of Work (manages all repositories and transactions)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register Repositories (can still be used individually if needed)
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ISellerRepository, SellerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IDiscountRepository, DiscountRepository>();
            services.AddScoped<IStoreShippingConfigurationRepository, StoreShippingConfigurationRepository>();
            services.AddScoped<ICountryRepository, CountryRepository>(); // Location-based shipping
            services.AddScoped<IGovernorateRepository, GovernorateRepository>(); // Location-based shipping
            services.AddScoped<ICityRepository, CityRepository>(); // Location-based shipping (city-governorate resolution)
            services.AddScoped<IStoreGovernorateSupportRepository, StoreGovernorateSupportRepository>(); // Store-governorate junction table

            // Register Location Management Services
            services.AddScoped<ICountryManagementService, CountryManagementService>();
            services.AddScoped<IGovernorateManagementService, GovernorateManagementService>();
            services.AddScoped<ICityManagementService, CityManagementService>();

            // Register Core Services
            services.AddScoped<IJwtService, JwtService>();
            
            // Register Store Services (SOLID principle separation)
            services.AddScoped<IStoreAuthorizationService, StoreAuthorizationService>();
            services.AddScoped<IStoreValidationService, StoreValidationService>();
            services.AddScoped<IStoreQueryService, StoreQueryService>();
            services.AddScoped<IStoreAnalyticsService, StoreAnalyticsService>();
            services.AddScoped<IStoreManagementService, StoreManagementService>();
            services.AddScoped<IStoreShippingConfigurationService, StoreShippingConfigurationService>();
            services.AddScoped<IStoreService, StoreService>(); // Composite interface
            
            // Register Product Services (SOLID principle separation)
            services.AddScoped<IProductManagementService, ProductManagementService>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<IProductInventoryService, ProductInventoryService>();
            services.AddScoped<IProductAnalyticsService, ProductAnalyticsService>();
            services.AddScoped<IProductValidationService, ProductValidationService>();
            services.AddScoped<IProductService, ProductService>(); // Composite interface
            
            // Register Order Services (SOLID principle separation)
            services.AddScoped<IOrderManagementService, OrderManagementService>();
            services.AddScoped<IOrderQueryService, OrderQueryService>();
            services.AddScoped<IOrderValidationService, OrderValidationService>();
            services.AddScoped<IOrderAnalyticsService, OrderAnalyticsService>();
            services.AddScoped<IOrderPaymentService, OrderPaymentService>();
            services.AddScoped<IOrderService, OrderService>(); // Composite interface
            
            // Register Inventory Services (SOLID principle separation)
            services.AddScoped<IInventoryManagementService, InventoryManagementService>();
            services.AddScoped<IInventoryQueryService, InventoryQueryService>();
            services.AddScoped<IInventoryValidationService, InventoryValidationService>();
            services.AddScoped<IInventoryAnalyticsService, InventoryAnalyticsService>();
            services.AddScoped<IInventoryAlertService, InventoryAlertService>();
            services.AddScoped<IInventoryService, InventoryService>(); // Composite interface

            // Register Discount Services (SOLID principle separation)
            services.AddScoped<IDiscountManagementService, DiscountManagementService>();
            services.AddScoped<IDiscountValidationService, DiscountValidationService>();
            services.AddScoped<IDiscountAnalyticsService, DiscountAnalyticsService>();
            services.AddScoped<IDiscountService, DiscountService>(); // Composite interface

            // Configure EmailSettings
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            
            // Register Email Services with factory pattern
            services.AddScoped<IEmailTemplateService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<EmailTemplateService>>();
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>();

                var templatesPath = !string.IsNullOrEmpty(emailSettings.Value.TemplatesPath)
                    ? Path.Combine(env.ContentRootPath, emailSettings.Value.TemplatesPath)
                    : Path.Combine(env.ContentRootPath, "..", "Bazario.Core", "Templates");

                return new EmailTemplateService(logger, templatesPath);
            });
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailSender, EmailSender>();

            // Register Helper Classes (Business Logic Extraction)
            services.AddScoped<ITokenHelper, TokenHelper>();
            services.AddScoped<IUserCreationService, UserCreationService>();
            services.AddScoped<IRoleManagementHelper, RoleManagementHelper>();
            services.AddScoped<IEmailHelper, EmailHelper>();
            services.AddScoped<IShippingZoneService, ShippingZoneService>();
            services.AddScoped<IOrderMetricsHelper, OrderMetricsHelper>();
            services.AddScoped<OrderCalculator>(); // Order calculation helper (KISS refactoring)
            services.AddScoped<IProductValidationHelper, ProductValidationHelper>();
            services.AddScoped<IInventoryHelper, InventoryHelper>();
            services.AddScoped<IStoreShippingConfigurationHelper, StoreShippingConfigurationHelper>();

            // Register Service Aggregators (Dependency Bundling)
            services.AddScoped<IUserAuthenticationDependencies, UserAuthenticationDependencies>();
            services.AddScoped<IUserRegistrationDependencies, UserRegistrationDependencies>();
            
            // Register Auth Services (Focused Services - Dependencies First)
            services.AddScoped<IUserRegistrationService, UserRegistrationService>();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            
            // Register Coordinating Auth Service (Depends on Focused Services)
            services.AddScoped<IAuthService, AuthService>();

            // Memory Cache removed - using database for refresh token storage

            // Add JWT Bearer Authentication (Single, clean configuration)
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"] ?? "")),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            services.AddAuthorization(options => {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
                options.AddPolicy("Seller", policy => policy.RequireRole("Seller"));
            });

            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
    }
}
