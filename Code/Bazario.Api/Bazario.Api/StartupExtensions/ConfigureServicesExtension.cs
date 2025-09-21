using Bazario.Infrastructure.DbContext;
using Bazario.Infrastructure.DbContext.Configurations;
using Bazario.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Bazario.Core.Helpers.Store;
using Bazario.Core.ServiceContracts.Store;
using Bazario.Core.ServiceContracts.Product;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;

using Bazario.Core.Services.Store;
using Bazario.Core.Services.Product;
using Bazario.Core.Models.Email;
using Bazario.Core.ServiceContracts.Auth;
using Bazario.Core.Services.Auth;
using Bazario.Core.ServiceContracts.Email;
using Bazario.Core.Services.Email;
using Bazario.Core.Helpers.Auth;
using Bazario.Core.Helpers.Email;

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

            // Register Repositories
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ISellerRepository, SellerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Register Core Services
            services.AddScoped<IJwtService, JwtService>();
            
            // Register Store Services (SOLID principle separation)
            services.AddScoped<IStoreValidationService, StoreValidationService>();
            services.AddScoped<IStoreQueryService, StoreQueryService>();
            services.AddScoped<IStoreAnalyticsService, StoreAnalyticsService>();
            services.AddScoped<IStoreManagementService, StoreManagementService>();
            services.AddScoped<IStoreService, StoreService>(); // Composite interface
            
            // Register Store Helpers
            services.AddScoped<IStoreManagementHelper, StoreManagementHelper>();
            
            // Register Product Services (SOLID principle separation)
            services.AddScoped<IProductManagementService, ProductManagementService>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<IProductInventoryService, ProductInventoryService>();
            services.AddScoped<IProductAnalyticsService, ProductAnalyticsService>();
            services.AddScoped<IProductValidationService, ProductValidationService>();
            services.AddScoped<IProductService, ProductService>(); // Composite interface
            
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
                    : Path.Combine(env.ContentRootPath, "..", "Bazario.Email", "Templates");
                
                return new EmailTemplateService(logger, templatesPath);
            });
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailSender, EmailSender>();

            // Register Helper Classes (Business Logic Extraction)
            services.AddScoped<ITokenHelper, TokenHelper>();
            services.AddScoped<IUserCreationService, UserCreationService>();
            services.AddScoped<IRoleManagementHelper, RoleManagementHelper>();
            services.AddScoped<IEmailHelper, EmailHelper>();
            
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
