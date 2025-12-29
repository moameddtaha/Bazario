# Bazario E-Commerce Platform API

A comprehensive, production-ready RESTful API for a multi-vendor e-commerce platform built with ASP.NET Core 8.0, following Clean Architecture principles.

## 🚀 Features

### Authentication & Authorization
- **JWT-based authentication** with access and refresh tokens
- **Role-based authorization** (Admin, Seller, Customer)
- Email verification and password reset functionality
- Secure password hashing with BCrypt
- Token refresh and revocation mechanisms

### Multi-Vendor Marketplace
- **Store Management**: Sellers can create and manage multiple stores
- **Product Catalog**: Comprehensive product management with categories
- **Inventory Tracking**: Real-time stock management with movement history
- **Order Processing**: Complete order lifecycle management
- **Review System**: Product and store reviews with ratings

### Advanced Features
- **Discount System**: Percentage and fixed-amount discounts with validation
- **Shipping Configuration**: Per-store shipping zones, fees, and delivery options
- **Same-Day Delivery**: Configurable cutoff times and availability checking
- **Location-Based Shipping**: Egyptian governorates and cities with custom rates
- **Inventory Alerts**: Low stock, out-of-stock, and restock notifications
- **Analytics**: Store and product performance metrics

## 🏗️ Architecture

### Clean Architecture Layers

```
Bazario.Api/
├── Bazario.Api/              # Presentation Layer (Controllers, Middleware)
├── Bazario.Core/             # Application & Domain Layer (Services, Entities, DTOs)
└── Bazario.Infrastructure/   # Infrastructure Layer (Repositories, DbContext, External Services)
```

### Design Patterns
- **Repository Pattern**: Data access abstraction
- **Unit of Work Pattern**: Transaction management
- **Dependency Injection**: Loose coupling and testability
- **Service Layer Pattern**: Business logic separation
- **DTO Pattern**: Data transfer objects for API contracts

### Key Technologies
- **ASP.NET Core 8.0**: Modern web framework
- **Entity Framework Core**: ORM for database operations
- **SQL Server**: Primary database (LocalDB for development)
- **Serilog**: Structured logging to Console, File, and Database
- **Swagger/OpenAPI**: API documentation and testing
- **API Versioning**: Versioned endpoints (v1.0)
- **DotNetEnv**: Environment variable management

## 📁 Project Structure

### Bazario.Api (Presentation Layer)
```
Controllers/
├── v1/
│   ├── Admin/              # Admin-only endpoints
│   ├── Auth/               # Authentication endpoints (Public)
│   ├── Discount/           # Discount management (Public, Seller, Admin)
│   ├── Inventory/          # Inventory management (Seller, Admin)
│   ├── Location/           # Geographic data (Public, Admin)
│   ├── Order/              # Order processing (Customer, Seller, Admin)
│   ├── Product/            # Product catalog (Public, Seller, Admin)
│   ├── Review/             # Review system (Public, Customer, Admin)
│   ├── Seller/             # Seller-specific endpoints
│   ├── Shipping/           # Shipping configuration (Public, Seller, Admin)
│   └── Store/              # Store management (Public, Seller, Admin)
StartupExtensions/
├── ConfigureServicesExtension.cs    # Dependency injection configuration
```

### Bazario.Core (Application & Domain Layer)
```
Domain/                     # Domain entities
├── Entities/
│   ├── User.cs
│   ├── Store.cs
│   ├── Product.cs
│   ├── Order.cs
│   ├── Inventory.cs
│   ├── Review.cs
│   ├── Discount.cs
│   └── Location/ (Country, Governorate, City)
Enums/                      # Enumerations
DTO/                        # Data Transfer Objects
│   ├── Auth/
│   ├── Store/
│   ├── Product/
│   ├── Order/
│   └── ...
ServiceContracts/           # Service interfaces
│   ├── Auth/
│   ├── Store/
│   ├── Product/
│   └── ...
Services/                   # Service implementations
Exceptions/                 # Custom exceptions
Helpers/                    # Utility classes
Templates/                  # Email templates
```

### Bazario.Infrastructure (Infrastructure Layer)
```
Data/
├── ApplicationDbContext.cs
├── Repositories/
└── Configurations/         # EF Core entity configurations
```

## 🔐 Authentication Flow

### Registration
1. User registers with email and password
2. System sends verification email with token
3. User verifies email via token
4. Account becomes active

### Login
1. User authenticates with email/password
2. System validates credentials
3. JWT access token (15 min) and refresh token (7 days) generated
4. Client stores tokens for subsequent requests

### Password Reset
1. User requests password reset via email
2. System sends reset token via email
3. User submits new password with token
4. Password updated, all tokens revoked

## 📡 API Endpoints

### Authentication (Public)
- `POST /api/v1/auth/register` - Register new user
- `POST /api/v1/auth/login` - Login and get JWT tokens
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/revoke` - Revoke refresh token
- `POST /api/v1/auth/forgot-password` - Request password reset
- `POST /api/v1/auth/reset-password` - Reset password with token
- `POST /api/v1/auth/verify-email` - Verify email with token

### Stores
#### Public
- `GET /api/v1/stores` - Get all active stores
- `GET /api/v1/stores/{id}` - Get store by ID
- `GET /api/v1/stores/search?query=` - Search stores

#### Seller
- `GET /api/v1/seller/stores` - Get seller's stores
- `POST /api/v1/seller/stores` - Create new store
- `PUT /api/v1/seller/stores/{id}` - Update store
- `DELETE /api/v1/seller/stores/{id}` - Soft delete store
- `POST /api/v1/seller/stores/{id}/activate` - Activate store
- `POST /api/v1/seller/stores/{id}/deactivate` - Deactivate store

#### Admin
- `GET /api/v1/admin/stores` - Get all stores (including deleted)
- `DELETE /api/v1/admin/stores/{id}` - Hard delete store (with reason)

### Products
Similar structure: Public (read-only), Seller (CRUD for own products), Admin (full access)

### Orders
- Customer: Create, view own orders, cancel pending orders
- Seller: View store orders, update status
- Admin: View all orders, analytics

### Shipping Configuration
#### Public
- `GET /api/v1/shipping/stores/{storeId}/same-day-availability?city=` - Check same-day delivery
- `GET /api/v1/shipping/stores/{storeId}/delivery-fee?city=` - Calculate delivery fee
- `GET /api/v1/shipping/stores/{storeId}/delivery-options?city=` - Get delivery options
- `GET /api/v1/shipping/stores/{storeId}/shipping-zone?city=` - Get shipping zone

#### Seller
- `GET /api/v1/seller/shipping/stores/{storeId}/configuration` - Get configuration
- `POST /api/v1/seller/shipping/configuration` - Create configuration
- `PUT /api/v1/seller/shipping/configuration` - Update configuration

#### Admin
- Full CRUD access to all store shipping configurations
- `DELETE /api/v1/admin/shipping/stores/{storeId}/configuration?reason=` - Delete configuration

### Discounts
- Public: Validate discount codes
- Seller: CRUD for store-specific discounts
- Admin: CRUD for global and store-specific discounts

### Inventory
- Seller: View stock, restock, adjust inventory
- Admin: Full inventory management and analytics

### Location
#### Public
- `GET /api/v1/location/countries` - Get all countries
- `GET /api/v1/location/countries/{countryId}/governorates` - Get governorates
- `GET /api/v1/location/governorates/{governorateId}/cities` - Get cities

#### Admin
- Full CRUD for countries, governorates, and cities

### Reviews
- Public: View reviews
- Customer: Create, update, delete own reviews
- Admin: Moderate all reviews

## ⚙️ Configuration

### Environment Variables (.env)
```env
# JWT Configuration
JwtSettings__SecretKey=<base64-encoded-256-bit-key>
JwtSettings__Issuer=https://bazario-api.com
JwtSettings__Audience=https://bazario-api.com
JwtSettings__AccessTokenExpirationMinutes=15
JwtSettings__RefreshTokenExpirationDays=7

# Database Connection
ConnectionStrings__DefaultConnection=Server=(localdb)\\mssqllocaldb;Database=BazarioDb;Trusted_Connection=true;MultipleActiveResultSets=true

# App Settings
AppSettings__EmailConfirmationUrl=https://your-frontend-domain.com/confirm-email
AppSettings__PasswordResetUrl=https://your-frontend-domain.com/reset-password

# Gmail SMTP Settings
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__Username=your-email@gmail.com
EmailSettings__Password=your-app-password
EmailSettings__EnableSsl=true
EmailSettings__FromEmail=your-email@gmail.com
EmailSettings__FromName=Bazario App
```

### appsettings.json
Contains fallback values and logging configuration. Sensitive values should be stored in `.env`.

## 🗄️ Database

### Entity Framework Core Migrations
```bash
# Create new migration
dotnet ef migrations add <MigrationName> --project Bazario.Infrastructure --startup-project Bazario.Api

# Update database
dotnet ef database update --project Bazario.Infrastructure --startup-project Bazario.Api

# Drop database (development only)
dotnet ef database drop --project Bazario.Infrastructure --startup-project Bazario.Api
```

### Key Entities
- **User**: Authentication and user management
- **Seller/Customer/Admin**: User role entities
- **Store**: Multi-vendor stores
- **Product**: Product catalog with categories
- **Inventory**: Stock management with movement tracking
- **Order/OrderItem**: Order processing
- **Discount**: Promotional codes
- **Review**: Product and store reviews
- **Country/Governorate/City**: Location hierarchy
- **StoreShippingConfiguration**: Per-store shipping settings
- **InventoryAlertPreferences**: Inventory notification settings

## 📊 Logging

### Serilog Configuration
- **Console Sink**: Development debugging
- **File Sink**: Rolling daily logs (30-day retention, 10MB size limit)
- **MSSqlServer Sink**: Production logs database (warnings and errors only)

### Log Enrichment
- Machine name
- Process ID
- Thread ID
- User name (authenticated requests)
- User agent
- IP address

### Log Locations
- Console: Real-time output
- File: `Logs/Prod/log-YYYYMMDD.txt`
- Database: `BazarioLogs.ProductionLogs` table

## 🧪 Testing

### Test Projects
- **Bazario.Auth.ServiceTests**: Authentication service unit tests
- **Bazario.Email.ServiceTests**: Email service unit tests

### Running Tests
```bash
dotnet test
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server or LocalDB
- Visual Studio 2022 / VS Code / Rider

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd Bazario.Api
```

2. **Configure environment variables**
```bash
# Copy .env.example to .env and update values
cp .env.example .env
```

3. **Generate JWT Secret Key** (PowerShell)
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```
Add the output to `.env` as `JwtSettings__SecretKey`

4. **Update database connection string** in `.env`

5. **Run migrations**
```bash
dotnet ef database update --project Bazario.Infrastructure --startup-project Bazario.Api
```

6. **Build and run**
```bash
dotnet build
dotnet run --project Bazario.Api
```

7. **Access Swagger UI** (Development only)
```
https://localhost:5001/swagger
```

## 📝 API Versioning

The API uses URL-based versioning:
- Current version: `v1.0`
- Base URL: `/api/v{version}/`
- Example: `/api/v1/stores`

## 🔒 Security

### Best Practices Implemented
- ✅ JWT token authentication
- ✅ Password hashing with BCrypt
- ✅ Email verification required
- ✅ Role-based authorization
- ✅ HTTPS enforcement
- ✅ CORS configuration
- ✅ Input validation with Data Annotations
- ✅ SQL injection prevention (EF Core parameterization)
- ✅ XSS prevention (automatic encoding)
- ✅ Audit trails (CreatedBy, UpdatedBy, timestamps)

### Security Headers
- HTTPS redirection enabled
- Sensitive data excluded from logs

## 📚 Documentation

### Additional Documentation
- [Entity Relationship Diagram (ERD.md)](ERD.md) - Complete database schema
- [Class Diagram (ClassDiagram.md)](ClassDiagram.md) - System architecture

### Swagger/OpenAPI
API documentation available at `/swagger` in development environment.

## 🤝 Contributing

### Code Style
- Follow C# naming conventions
- Use async/await for I/O operations
- Include XML documentation comments
- Write unit tests for business logic

### Git Workflow
1. Create feature branch: `git checkout -b feature/your-feature`
2. Commit changes: `git commit -m "feat: description"`
3. Push to branch: `git push origin feature/your-feature`
4. Create Pull Request

### Commit Message Convention
- `feat:` New feature
- `fix:` Bug fix
- `refactor:` Code refactoring
- `docs:` Documentation changes
- `test:` Test additions/modifications

## 📄 License

This project is proprietary software. All rights reserved.

## 👥 Authors

- Mohamed Taha - Initial development

## 🎯 Roadmap

### Completed Features ✅
- Phase 1: Authentication system
- Phase 2: Store management
- Phase 3: Product catalog
- Phase 4: Order processing
- Phase 5: Store controllers (Public, Seller, Admin)
- Phase 6: Discount controllers
- Phase 7: Inventory management
- Phase 8: Location management
- Phase 9: Shipping configuration
- Phase 10: Review system

### Future Enhancements 🔮
- Payment gateway integration (Paymob, Stripe)
- Real-time notifications (SignalR)
- Image upload with cloud storage (Azure Blob, AWS S3)
- Advanced analytics dashboard
- Wishlist functionality
- Product recommendations
- Multi-language support
- Mobile app API optimizations
- Caching layer (Redis)
- Rate limiting
- GraphQL endpoint

## 🐛 Known Issues

None at this time.

## 📞 Support

For questions or issues, please create an issue in the repository.

---

**Built with ❤️ using ASP.NET Core 8.0**

*Last Updated: December 2025*
