# Bazario E-Commerce Platform

A modern, scalable multi-vendor e-commerce platform built with .NET 8 and Clean Architecture principles.

## 🚀 Overview

Bazario is a comprehensive e-commerce solution that enables sellers to create stores, manage products, and process orders while providing customers with a seamless shopping experience. The platform features advanced inventory management, location-based shipping, multi-discount support, and real-time analytics.

## ✨ Key Features

### For Sellers
- **Multi-Store Management** - Create and manage multiple stores with individual analytics
- **Product Management** - Full CRUD operations with inventory tracking
- **Order Processing** - Automated order lifecycle management (Pending → Processing → Shipped → Delivered)
- **Inventory Control** - Real-time stock tracking, low stock alerts, and forecasting
- **Analytics Dashboard** - Revenue tracking, sales metrics, and performance insights
- **Shipping Configuration** - Location-based shipping rates per governorate

### For Customers
- **Product Discovery** - Advanced search and filtering capabilities
- **Shopping Cart** - Multi-store cart with automatic calculation
- **Multi-Discount Support** - Apply multiple discount codes with proportional attribution
- **Location-Based Shipping** - Accurate shipping costs based on governorate
- **Order Tracking** - Real-time order status updates
- **Product Reviews** - Rate and review purchased products

### For Administrators
- **User Management** - Manage sellers and customers
- **Platform Analytics** - System-wide metrics and reporting
- **Content Moderation** - Review and moderate content
- **Discount Management** - Create and track promotional campaigns

## 🏗️ Architecture

### Clean Architecture Layers

```
Bazario.Api/
├── Bazario.Api/              # Presentation Layer (REST API)
├── Bazario.Core/             # Domain Layer (Business Logic)
│   ├── Domain/               # Entities, Enums, Interfaces
│   ├── Services/             # Business Logic Services
│   ├── ServiceContracts/     # Service Interfaces
│   ├── Models/               # DTOs and View Models
│   └── Helpers/              # Utility Classes
├── Bazario.Infrastructure/   # Infrastructure Layer (Data Access)
│   ├── Repositories/         # Repository Implementations
│   ├── DbContext/            # Entity Framework Context
│   ├── Configurations/       # EF Configurations
│   └── Migrations/           # Database Migrations
```

### Domain-Driven Design (DDD)

The project follows vertical slice architecture organized by domain:

- **Authentication** - User registration, login, JWT tokens
- **Catalog** - Products, categories, discounts
- **Store** - Store management and analytics
- **Order** - Order processing and payment
- **Inventory** - Stock management and alerts
- **Review** - Product reviews and ratings
- **Location** - Countries, governorates, cities

### SOLID Principles

Services are separated by responsibility:
- **Management Services** - CRUD operations
- **Validation Services** - Business rule validation
- **Query Services** - Read operations and filtering
- **Analytics Services** - Reporting and metrics
- **Composite Services** - Unified interface

## 🛠️ Technology Stack

### Backend
- **.NET 8** - Latest LTS version
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM for database access
- **SQL Server** - Relational database
- **AutoMapper** - Object-to-object mapping
- **Serilog** - Structured logging

### Authentication & Security
- **JWT (JSON Web Tokens)** - Stateless authentication
- **BCrypt** - Password hashing
- **Role-based Authorization** - Admin, Seller, Customer roles

### Design Patterns
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management
- **Dependency Injection** - Loose coupling
- **CQRS** - Separation of reads and writes
- **Specification Pattern** - Query logic encapsulation

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server Express
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/moameddtaha/Bazario
cd bazario/Bazario.Api
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=BazarioDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Apply Database Migrations

```bash
dotnet ef database update --project Bazario.Infrastructure --startup-project Bazario.Api
```

### 4. Run the Application

```bash
dotnet run --project Bazario.Api
```

The API will be available at:
- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:5001`
- **Swagger UI:** `https://localhost:5001/swagger`

## 📚 API Documentation

### Authentication Endpoints

```
POST   /api/auth/register        # Register new user
POST   /api/auth/login           # Login and get JWT token
POST   /api/auth/refresh         # Refresh access token
POST   /api/auth/forgot-password # Request password reset
POST   /api/auth/reset-password  # Reset password
GET    /api/auth/me              # Get current user info
PUT    /api/auth/change-password # Change password
```

### Store Management Endpoints

```
GET    /api/stores               # List all stores
POST   /api/stores               # Create new store
GET    /api/stores/{id}          # Get store details
PUT    /api/stores/{id}          # Update store
DELETE /api/stores/{id}          # Delete store
GET    /api/stores/{id}/products # Get store products
GET    /api/stores/{id}/analytics # Get store analytics
```

### Product Management Endpoints

```
GET    /api/products             # List products (with filtering)
POST   /api/products             # Create product
GET    /api/products/{id}        # Get product details
PUT    /api/products/{id}        # Update product
DELETE /api/products/{id}        # Delete product
GET    /api/products/{id}/reviews # Get product reviews
```

### Order Management Endpoints

```
GET    /api/orders               # List orders (with filtering)
POST   /api/orders               # Create order
GET    /api/orders/{id}          # Get order details
PUT    /api/orders/{id}          # Update order status
POST   /api/orders/{id}/cancel   # Cancel order
GET    /api/orders/analytics     # Get order analytics
```

### Inventory Management Endpoints

```
GET    /api/inventory            # List inventory items
GET    /api/inventory/{productId} # Get product stock
PUT    /api/inventory/{productId} # Update stock levels
GET    /api/inventory/alerts     # Get low stock alerts
GET    /api/inventory/reports    # Get inventory reports
```

### Discount Management Endpoints

```
GET    /api/discounts            # List discount codes
POST   /api/discounts            # Create discount
GET    /api/discounts/{id}       # Get discount details
PUT    /api/discounts/{id}       # Update discount
DELETE /api/discounts/{id}       # Delete discount
POST   /api/discounts/validate   # Validate discount code
```

For complete API documentation, visit the **Swagger UI** at `/swagger` when running the application.

## 🗄️ Database Schema

### Core Entities

- **Users** - Customer and seller accounts
- **Roles** - Admin, Seller, Customer
- **Stores** - Seller stores
- **Products** - Store products
- **Categories** - Product categories
- **Orders** - Customer orders
- **OrderItems** - Individual order line items
- **Inventory** - Product stock levels
- **Discounts** - Promotional codes
- **Reviews** - Product reviews and ratings
- **Countries** - Supported countries
- **Governorates** - States/provinces
- **Cities** - Cities for shipping

## 🔐 Authentication & Authorization

### JWT Configuration

The application uses JWT tokens with the following claims:
- `sub` - User ID
- `email` - User email
- `role` - User role (Admin, Seller, Customer)
- `exp` - Token expiration

### Role-Based Access

- **Admin** - Full system access
- **Seller** - Manage own stores, products, and orders
- **Customer** - Browse products, place orders, write reviews

### Example: Protected Endpoint

```csharp
[Authorize(Roles = "Seller")]
[HttpPost]
public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request)
{
    // Only sellers can create stores
}
```

## 📊 Business Features

### Order Calculation System

Automated order total calculation with:
- **Subtotal Calculation** - Sum of all product prices
- **Multi-Discount Support** - Apply multiple codes with proportional attribution
- **Location-Based Shipping** - Calculate shipping by governorate
- **Tax Calculation** - Configurable tax rates (currently disabled)

### Discount System

- **Percentage Discounts** - 10% off, 25% off, etc.
- **Fixed Amount Discounts** - $10 off, $50 off, etc.
- **Minimum Order Requirements** - Apply only above threshold
- **Store-Specific Discounts** - Limit to specific stores
- **Global Discounts** - Platform-wide promotions
- **One-Time Use** - Single-use discount codes
- **Date-Based Validity** - Valid from/to dates

### Location-Based Shipping

- **27 Egyptian Governorates** - Complete coverage
- **Major Cities** - Cairo, Alexandria, Giza, etc.
- **Express Delivery** - Available in major cities
- **Same-Day Delivery** - Available in Cairo
- **Free Shipping Threshold** - Configurable per store
- **Hierarchical Resolution** - Postal Code → City → Governorate → Country

### Inventory Management

- **Real-Time Stock Tracking** - Automatic updates on orders
- **Low Stock Alerts** - Configurable thresholds
- **Stock Reservations** - Temporary holds during checkout
- **Bulk Stock Updates** - Import stock levels
- **Stock Forecasting** - Demand prediction
- **Inventory Valuation** - Total inventory value
- **Dead Stock Detection** - Identify slow-moving items

## 🧪 Testing

### Unit Tests

```bash
dotnet test Bazario.Tests.Unit
```

### Integration Tests

```bash
dotnet test Bazario.Tests.Integration
```

### Test Coverage

Run tests with coverage:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🚢 Deployment

### Production Checklist

- [ ] Update connection strings for production database
- [ ] Configure JWT secret key
- [ ] Enable HTTPS
- [ ] Configure CORS policies
- [ ] Set up logging and monitoring
- [ ] Configure email service (SMTP)
- [ ] Set up payment gateway (Paymob)
- [ ] Enable rate limiting
- [ ] Configure CDN for static assets
- [ ] Set up backup and recovery

### Docker Deployment

```bash
docker build -t bazario-api .
docker run -d -p 5000:80 bazario-api
```

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
JWT__Secret=your-secret-key
ConnectionStrings__DefaultConnection=your-connection-string
SMTP__Host=smtp.gmail.com
SMTP__Port=587
SMTP__Username=your-email
SMTP__Password=your-password
```

## 📈 Performance Optimizations

- **AsNoTracking()** - For read-only queries
- **Database Indexing** - On frequently queried columns
- **Pagination** - For large result sets
- **Caching** - Redis for frequently accessed data
- **Lazy Loading** - Disabled to prevent N+1 queries
- **Bulk Operations** - For batch updates
- **Query Optimization** - Database-level filtering and aggregation

## 🔄 Migration Guide

### Adding a New Migration

```bash
dotnet ef migrations add MigrationName --project Bazario.Infrastructure --startup-project Bazario.Api
```

### Applying Migrations

```bash
dotnet ef database update --project Bazario.Infrastructure --startup-project Bazario.Api
```

### Rolling Back Migrations

```bash
dotnet ef database update PreviousMigrationName --project Bazario.Infrastructure --startup-project Bazario.Api
```

## 📝 Code Quality Standards

### KISS Principle
- Methods should be under 50 lines
- Single responsibility per method
- Avoid complex nested conditions

### SOLID Principles
- **S**ingle Responsibility - One reason to change
- **O**pen/Closed - Open for extension, closed for modification
- **L**iskov Substitution - Subtypes must be substitutable
- **I**nterface Segregation - Many specific interfaces vs one general
- **D**ependency Inversion - Depend on abstractions, not concretions

### Logging Levels
- **Debug** - Routine operations
- **Information** - State changes
- **Warning** - Validation failures
- **Error** - Exceptions and failures

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Commit Message Format

```
feat: Add new feature
fix: Fix bug in order calculation
refactor: Simplify StoreValidationService
docs: Update README
test: Add unit tests for DiscountService
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Mohamed Taha** - *Initial work*

## 🙏 Acknowledgments

- Clean Architecture principles by Robert C. Martin
- .NET team for excellent documentation
- Community contributors

## 📞 Support

For support, email support@bazario.com or create an issue in this repository.

## 🗺️ Roadmap

### Version 1.0 (Current)
- [x] Core authentication system
- [x] Store and product management
- [x] Order processing system
- [x] Inventory management
- [x] Discount system
- [x] Location-based shipping
- [ ] Review and rating system
- [ ] Admin dashboard
- [ ] Payment gateway integration (Paymob)

### Version 2.0 (Planned)
- [ ] Real-time notifications
- [ ] Advanced search with Elasticsearch
- [ ] Recommendation engine
- [ ] Multi-language support
- [ ] Mobile app (Flutter)
- [ ] Seller analytics dashboard
- [ ] Automated email campaigns
- [ ] Chat support system

---

**Built with ❤️ using .NET 8 and Clean Architecture**
