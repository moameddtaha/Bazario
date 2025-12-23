using Bazario.Infrastructure.DbContext;
using Bazario.Infrastructure.DbContext.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using Asp.Versioning;

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
using Bazario.Core.Helpers.Store;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Domain.RepositoryContracts.Authentication;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Inventory;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts.Review;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Core.Helpers.Authentication;
using Bazario.Core.Helpers.Infrastructure;
using Bazario.Core.Models.Infrastructure;
using Bazario.Core.ServiceContracts.Authentication;
using Bazario.Core.ServiceContracts.Authorization;
using Bazario.Core.ServiceContracts.Catalog.Product;
using Bazario.Core.ServiceContracts.Catalog.Discount;
using Bazario.Core.ServiceContracts.Review;
using Bazario.Core.ServiceContracts.Infrastructure;
using Bazario.Core.Services.Authentication;
using Bazario.Core.Services.Authorization;
using Bazario.Core.Services.Catalog.Product;
using Bazario.Core.Services.Catalog.Discount;
using Bazario.Core.Services.Review;
using Bazario.Core.Services.Infrastructure;
using Bazario.Infrastructure.Repositories.Authentication;
using Bazario.Infrastructure.Repositories.UserManagement;
using Bazario.Infrastructure.Repositories.Catalog;
using Bazario.Infrastructure.Repositories.Inventory;
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

            // Add Memory Cache
            services.AddMemoryCache();

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
            services.AddScoped<IStockReservationRepository, StockReservationRepository>(); // Stock reservation system
            services.AddScoped<IInventoryAlertPreferencesRepository, InventoryAlertPreferencesRepository>(); // Inventory alert preferences persistence

            // ========================================
            // LOCATION MANAGEMENT SERVICES
            // ========================================
            services.AddScoped<ICountryManagementService, CountryManagementService>();
            services.AddScoped<IGovernorateManagementService, GovernorateManagementService>();
            services.AddScoped<ICityManagementService, CityManagementService>();

            // ========================================
            // AUTHENTICATION SERVICES
            // ========================================
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUserCreationService, UserCreationService>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();

            // Service Aggregators (Dependency Bundling)
            services.AddScoped<IUserAuthenticationDependencies, UserAuthenticationDependencies>();
            services.AddScoped<IUserRegistrationDependencies, UserRegistrationDependencies>();

            services.AddScoped<IUserRegistrationService, UserRegistrationService>();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IAuthService, AuthService>(); // Coordinating service

            // ========================================
            // AUTHORIZATION SERVICES
            // ========================================
            services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();

            // ========================================
            // STORE SERVICES
            // ========================================
            services.AddScoped<IStoreAuthorizationService, StoreAuthorizationService>();
            services.AddScoped<IStoreValidationService, StoreValidationService>();
            services.AddScoped<IStoreQueryService, StoreQueryService>();
            services.AddScoped<IStoreAnalyticsService, StoreAnalyticsService>();
            services.AddScoped<IStoreManagementService, StoreManagementService>();
            services.AddScoped<IStoreShippingConfigurationService, StoreShippingConfigurationService>();
            services.AddScoped<IStoreService, StoreService>(); // Composite interface

            // ========================================
            // PRODUCT SERVICES
            // ========================================
            services.AddScoped<IProductManagementService, ProductManagementService>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<IProductAnalyticsService, ProductAnalyticsService>();
            services.AddScoped<IProductValidationService, ProductValidationService>();
            services.AddScoped<IProductService, ProductService>(); // Composite interface

            // ========================================
            // ORDER SERVICES
            // ========================================
            services.AddScoped<IOrderManagementService, OrderManagementService>();
            services.AddScoped<IOrderQueryService, OrderQueryService>();
            services.AddScoped<IOrderValidationService, OrderValidationService>();
            services.AddScoped<IOrderAnalyticsService, OrderAnalyticsService>();
            services.AddScoped<IOrderPaymentService, OrderPaymentService>();
            services.AddScoped<IShippingZoneService, ShippingZoneService>();
            services.AddScoped<IOrderCalculationService, OrderCalculationService>();
            services.AddScoped<IOrderService, OrderService>(); // Composite interface

            // ========================================
            // INVENTORY SERVICES
            // ========================================
            services.AddScoped<IInventoryManagementService, InventoryManagementService>();
            services.AddScoped<IInventoryQueryService, InventoryQueryService>();
            services.AddScoped<IInventoryValidationService, InventoryValidationService>();
            services.AddScoped<IInventoryAnalyticsService, InventoryAnalyticsService>();
            services.AddScoped<IInventoryAlertService, InventoryAlertService>();
            services.AddScoped<IInventoryService, InventoryService>(); // Composite interface

            // ========================================
            // DISCOUNT SERVICES
            // ========================================
            services.AddScoped<IDiscountManagementService, DiscountManagementService>();
            services.AddScoped<IDiscountValidationService, DiscountValidationService>();
            services.AddScoped<IDiscountAnalyticsService, DiscountAnalyticsService>();
            services.AddScoped<IDiscountService, DiscountService>(); // Composite interface

            // ========================================
            // REVIEW SERVICES
            // ========================================
            services.AddScoped<IReviewManagementService, ReviewManagementService>();
            services.AddScoped<IReviewValidationService, ReviewValidationService>();
            services.AddScoped<IReviewAnalyticsService, ReviewAnalyticsService>();
            services.AddScoped<IReviewModerationService, ReviewModerationService>();
            services.AddScoped<IReviewService, ReviewService>(); // Composite interface

            // ========================================
            // INFRASTRUCTURE SERVICES
            // ========================================
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

            // ========================================
            // HELPER CLASSES (Pure Business Logic - No Database Access)
            // ========================================
            services.AddScoped<ITokenHelper, TokenHelper>();
            services.AddScoped<IEmailHelper, EmailHelper>();
            services.AddScoped<IOrderMetricsHelper, OrderMetricsHelper>();
            services.AddScoped<IStoreShippingConfigurationHelper, StoreShippingConfigurationHelper>();
            services.AddScoped<IConfigurationHelper, ConfigurationHelper>();
            services.AddScoped<IConcurrencyHelper, ConcurrencyHelper>();
            services.AddScoped<IProductValidationHelper, ProductValidationHelper>();

            // ========================================
            // JWT AUTHENTICATION CONFIGURATION
            // ========================================
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

            // ========================================
            // BACKGROUND SERVICES
            // ========================================
            services.AddHostedService<Bazario.Infrastructure.BackgroundServices.RefreshTokenCleanupService>();

            // ========================================
            // API VERSIONING
            // ========================================
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddControllers();

            // ========================================
            // SWAGGER/OPENAPI CONFIGURATION
            // ========================================
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Bazario API",
                    Version = "v1",
                    Description = "E-commerce platform API with comprehensive endpoints for customers, sellers, and administrators",
                    Contact = new OpenApiContact
                    {
                        Name = "Bazario Support",
                        Email = "support@bazario.com"
                    }
                });

                // Include XML comments for better Swagger documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // JWT Bearer authentication configuration for Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}
