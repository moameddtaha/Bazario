# Bazario Setup & Configuration Guides

**Last Updated:** January 2025

This document consolidates all setup and configuration guides for the Bazario platform.

## Table of Contents
1. [Email Setup](#1-email-setup)
2. [Shipping Zone Expansion](#2-shipping-zone-expansion)
3. [API Usage Examples](#3-api-usage-examples)

---

# 1. Email Setup

## Overview
This section explains how to set up email functionality in your Bazario API using Gmail SMTP.

## Prerequisites

### 1.1 Gmail Account Setup
1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate an App Password**:
   - Go to Google Account settings
   - Security → 2-Step Verification → App passwords
   - Generate a new app password for "Mail"
   - Use this password in your configuration (NOT your regular Gmail password)

### 1.2 Required NuGet Packages
The following packages are already included:
- `MailKit` - For SMTP email functionality
- `DotNetEnv` - For environment variable management

## Configuration

### Environment Variables
Create a `.env` file in your project root with the following variables:

```bash
# Gmail SMTP Settings
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
ENABLE_SSL=true
FROM_EMAIL=your-email@gmail.com
FROM_NAME=Bazario Team

# App Settings
EMAIL_CONFIRMATION_URL=https://your-domain.com/confirm-email
PASSWORD_RESET_URL=https://your-domain.com/reset-password
```

### AppSettings Configuration
The email settings are automatically bound to the `EmailSettings` model from your configuration.

## Features

### 1.1 Password Reset Emails
- Sends HTML-formatted password reset emails
- Includes secure reset links with expiration
- Professional styling and branding

### 1.2 Email Confirmation
- Sends welcome emails with confirmation links
- 24-hour expiration for security
- Responsive HTML design

### 1.3 Security Features
- SSL/TLS encryption
- App password authentication
- Token-based verification
- Configurable timeouts

## Production Considerations

### 1.1 Email Service Providers
For production, consider using dedicated email services:
- **SendGrid** - High deliverability, analytics
- **Mailgun** - Developer-friendly, good pricing
- **Amazon SES** - Cost-effective for high volume
- **Postmark** - Transactional email specialist

### 1.2 Environment-Specific Configuration
- Use different email accounts for dev/staging/production
- Implement email templates for different environments
- Set up monitoring and alerting for email failures

### 1.3 Security Best Practices
- Never commit `.env` files to source control
- Use strong, unique app passwords
- Implement rate limiting for email sending
- Monitor for suspicious email activity

### 1.4 Monitoring and Logging
- All email operations are logged with Serilog
- Track email delivery success/failure rates
- Set up alerts for email service issues

## Testing

### Development Testing
- Use Gmail SMTP for development
- Test with real email addresses
- Verify email delivery and formatting

### Production Testing
- Test with production email configuration
- Verify deliverability to major email providers
- Check spam folder placement

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify 2FA is enabled
   - Use app password, not regular password
   - Check username format (email address)

2. **Connection Timeout**
   - Verify SMTP server and port
   - Check firewall settings
   - Ensure SSL/TLS is properly configured

3. **Emails Not Delivered**
   - Check spam/junk folders
   - Verify sender email address
   - Check Gmail sending limits

### Debug Mode
Enable debug logging in development to troubleshoot SMTP issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Bazario.Core.Services.EmailService": "Debug"
    }
  }
}
```

---

# 2. Shipping Management (Database-Driven)

## Overview
The shipping system is database-driven, allowing **admins** to manage countries and governorates, and **store owners** to configure which locations they ship to and their delivery fees.

## Current Implementation Status

### ✅ Completed
- Store-specific shipping configuration
- Delivery fee configuration per zone
- Supported/Excluded cities lists
- Same-day delivery toggle
- Egypt-focused with Cairo as primary

### 🔄 In Progress (Enhancements)
- Database-driven country/governorate management
- Admin panel for location management
- Store owner UI for shipping configuration

## Architecture

### Database Schema (Planned)

```
Countries
├── CountryId (PK)
├── Name (e.g., "Egypt")
├── Code (e.g., "EG")
├── IsActive
└── CreatedAt

Governorates/States
├── GovernorateId (PK)
├── CountryId (FK)
├── Name (e.g., "Cairo", "Giza")
├── Code (optional)
├── IsActive
└── CreatedAt

StoreShippingConfiguration
├── ConfigurationId (PK)
├── StoreId (FK)
├── SupportedGovernorates (comma-separated IDs)
├── ExcludedGovernorates (comma-separated IDs)
├── SupportedCities (comma-separated names) [existing]
├── ExcludedCities (comma-separated names) [existing]
├── SameDayDeliveryFee
├── StandardDeliveryFee
├── NationalDeliveryFee
└── ... (other fields)
```

## Admin Capabilities

### 1. Country Management

**Admins can:**
- ✅ Add new countries (Egypt, Saudi Arabia, UAE, etc.)
- ✅ Enable/disable countries for shipping
- ✅ Set country codes (EG, SA, AE, etc.)

**API Endpoints (Planned):**
```http
POST   /api/admin/countries           # Create country
GET    /api/admin/countries           # List all countries
GET    /api/admin/countries/{id}      # Get country details
PUT    /api/admin/countries/{id}      # Update country
DELETE /api/admin/countries/{id}      # Deactivate country
```

### 2. Governorate/State Management

**Admins can:**
- ✅ Add governorates/states to a country
- ✅ Enable/disable governorates
- ✅ Set governorate names in Arabic and English (future)

**API Endpoints (Planned):**
```http
POST   /api/admin/governorates                      # Create governorate
GET    /api/admin/countries/{countryId}/governorates # List governorates
PUT    /api/admin/governorates/{id}                 # Update governorate
DELETE /api/admin/governorates/{id}                 # Deactivate governorate
```

**Example: Egypt Governorates**
```json
[
  { "id": 1, "name": "Cairo", "countryId": 1 },
  { "id": 2, "name": "Giza", "countryId": 1 },
  { "id": 3, "name": "Alexandria", "countryId": 1 },
  { "id": 4, "name": "Qalyubia", "countryId": 1 },
  { "id": 5, "name": "Sharqia", "countryId": 1 }
]
```

## Store Owner Capabilities

### 1. Configure Shipping Zones

**Store owners can:**
- ✅ Select which governorates they ship to
- ✅ Exclude specific governorates
- ✅ Set delivery fees per zone:
  - Same-day delivery fee (Cairo only by default)
  - Standard delivery fee (local governorates)
  - National delivery fee (other governorates)
- ✅ Enable/disable same-day delivery
- ✅ Set cutoff time for same-day orders

**API Endpoints (Planned):**
```http
POST   /api/stores/{storeId}/shipping/configure     # Configure shipping
GET    /api/stores/{storeId}/shipping                # Get configuration
PUT    /api/stores/{storeId}/shipping                # Update configuration
GET    /api/governorates                             # List available governorates
```

### 2. Configuration Examples

#### Example 1: Cairo-Only Store
```json
{
  "storeId": "store-guid",
  "supportedGovernorates": [1],  // Cairo only
  "sameDayDeliveryFee": 50.00,
  "standardDeliveryFee": 30.00,
  "offersSameDayDelivery": true,
  "sameDayCutoffHour": 14
}
```

#### Example 2: Greater Cairo Store
```json
{
  "storeId": "store-guid",
  "supportedGovernorates": [1, 2, 4],  // Cairo, Giza, Qalyubia
  "standardDeliveryFee": 40.00,
  "nationalDeliveryFee": 60.00,
  "offersSameDayDelivery": false
}
```

#### Example 3: Nationwide Store
```json
{
  "storeId": "store-guid",
  "supportedGovernorates": [],  // Empty = all governorates
  "excludedGovernorates": [25, 26, 27],  // Exclude Sinai, Matruh, etc.
  "standardDeliveryFee": 50.00,
  "nationalDeliveryFee": 80.00
}
```

## Implementation Workflow

### Phase 1: Database Setup
1. Create `Country` entity
2. Create `Governorate` entity
3. Create repositories for both
4. Add migration
5. Seed Egyptian governorates

### Phase 2: Admin Services
1. Create `CountryManagementService`
2. Create `GovernorateManagementService`
3. Add validation logic
4. Register services in DI

### Phase 3: Store Owner Integration
1. Update `StoreShippingConfiguration` entity
2. Add `SupportedGovernorates` field
3. Add `ExcludedGovernorates` field
4. Update `StoreShippingConfigurationService`
5. Update `ShippingZoneService` to use database data

### Phase 4: API Controllers
1. Create `AdminCountriesController`
2. Create `AdminGovernoratesController`
3. Create `StoreShippingController`
4. Add authorization (Admin/Store Owner roles)

### Phase 5: Frontend Integration
1. Admin panel for country/governorate management
2. Store owner dashboard for shipping config
3. Multi-select dropdown for governorates
4. Delivery fee input fields

## Egyptian Governorates (Seed Data)

```sql
INSERT INTO Governorates (Name, CountryId, IsActive) VALUES
-- Greater Cairo
('Cairo', 1, 1),
('Giza', 1, 1),
('Qalyubia', 1, 1),

-- Delta
('Alexandria', 1, 1),
('Beheira', 1, 1),
('Gharbia', 1, 1),
('Kafr El Sheikh', 1, 1),
('Dakahlia', 1, 1),
('Damietta', 1, 1),
('Sharqia', 1, 1),
('Monufia', 1, 1),

-- Canal
('Port Said', 1, 1),
('Ismailia', 1, 1),
('Suez', 1, 1),

-- Upper Egypt
('Fayoum', 1, 1),
('Beni Suef', 1, 1),
('Minya', 1, 1),
('Asyut', 1, 1),
('Sohag', 1, 1),
('Qena', 1, 1),
('Luxor', 1, 1),
('Aswan', 1, 1),

-- Frontier
('Red Sea', 1, 1),
('New Valley', 1, 1),
('Matruh', 1, 1),
('North Sinai', 1, 1),
('South Sinai', 1, 1);
```

## Current Hardcoded Logic (To Be Migrated)

The current `ShippingZoneService` has hardcoded city lists:

```csharp
// Lines 76-90 in ShippingZoneService.cs
if (cityUpper == "CAIRO")
{
    return ShippingZone.SameDay;
}

if (cityUpper == "ALEXANDRIA" || cityUpper == "GIZA" || ...)
{
    return ShippingZone.National;
}
```

**Migration Plan:**
- Keep this as fallback logic
- Query database for governorate-based shipping
- Fallback to hardcoded if database query fails

## Benefits of Database Approach

### For Admins
✅ Easy expansion to new countries
✅ No code changes needed
✅ Centralized location management
✅ Support for localization (Arabic names)

### For Store Owners
✅ Visual governorate selection
✅ Easy configuration updates
✅ Clear shipping coverage
✅ Flexible whitelist/blacklist

### For Developers
✅ No hardcoded city lists
✅ Scalable architecture
✅ Easy maintenance
✅ Clean separation of concerns

## Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task GetSupportedGovernorates_ForStore_ReturnsConfiguredList()
{
    // Arrange
    var storeId = Guid.NewGuid();
    // Act
    var governorates = await _shippingService.GetSupportedGovernoratesAsync(storeId);
    // Assert
    Assert.Contains("Cairo", governorates);
}
```

### Integration Tests
- Test admin can create countries
- Test admin can add governorates
- Test store owner can configure shipping
- Test order calculation uses correct fees

## Migration from Current System

**Step 1:** Create database tables
**Step 2:** Seed Egyptian data
**Step 3:** Update services to query database
**Step 4:** Keep hardcoded fallback for safety
**Step 5:** Test thoroughly
**Step 6:** Create admin/store UI

**No Breaking Changes:** Existing `SupportedCities` and `ExcludedCities` will continue to work alongside the new governorate-based system.

---

# 3. Location-Based Shipping System Implementation

**Status:** 93% Complete (Phases 1-6 implemented, Phase 7 API Controllers deferred)
**Last Updated:** January 2025

## Overview
Complete database-driven location-based shipping system with junction table architecture, Egyptian seed data, and production-ready city resolution.

## Implementation Status

### ✅ Phase 1: Database Schema & Refactoring (100%)

**Entities Created:**
- `Country.cs` - Country entity with name, code, Arabic name support
- `Governorate.cs` - Governorate/State entity linked to countries
- `City.cs` - City entity linked to governorates (production-ready city resolution)
- `StoreGovernorateSupport.cs` - Junction table for many-to-many store-governorate relationship with `IsSupported` flag

**Repository Layer:**
- Full CRUD repositories with **safe update pattern** (only specific fields updatable)
- `ICountryRepository.cs` + `CountryRepository.cs`
- `IGovernorateRepository.cs` + `GovernorateRepository.cs`
- `ICityRepository.cs` + `CityRepository.cs` with search capabilities
- `IStoreGovernorateSupportRepository.cs` + `StoreGovernorateSupportRepository.cs` with bulk operations

**Database Context:**
- Added DbSets: Countries, Governorates, Cities, StoreGovernorateSupports
- EF Core configuration with composite unique index on (StoreId, GovernorateId)
- Proper foreign keys with cascade/restrict delete behaviors

**Legacy System Removal:**
- ✅ Removed `SupportedCities` and `ExcludedCities` comma-separated fields from `StoreShippingConfiguration`
- ✅ Replaced with junction table approach for referential integrity
- ✅ Added navigation properties to Store entity

### ✅ Phase 2: DTOs (100%)

**Location:** `Bazario.Core/DTO/Location/`

**Country DTOs:**
- `CountryAddRequest.cs` - [Required] Name, Code, NameArabic, SupportsPostalCodes
- `CountryUpdateRequest.cs` - [Required] CountryId, Name, IsActive
- `CountryResponse.cs` - Includes GovernorateCount

**Governorate DTOs:**
- `GovernorateAddRequest.cs` - [Required] CountryId, Name, SupportsSameDayDelivery
- `GovernorateUpdateRequest.cs` - [Required] GovernorateId, Name
- `GovernorateResponse.cs` - Includes CountryName, CityCount

**City DTOs:**
- `CityAddRequest.cs` - [Required] GovernorateId, Name, SupportsSameDayDelivery
- `CityUpdateRequest.cs` - [Required] CityId, Name
- `CityResponse.cs` - Includes GovernorateName, CountryName (hierarchical)

**Shipping DTOs:**
- `StoreShippingConfigurationRequest.cs` - Updated with `SupportedGovernorateIds`, `ExcludedGovernorateIds`
- `StoreShippingConfigurationResponse.cs` - Returns governorate collections with full details
- `GovernorateShippingInfo.cs` - Standalone DTO with governorate details

### ✅ Phase 3: Services (100%)

**Location:** `Bazario.Core/Services/Location/`

**Service Contracts:**
- `ICountryManagementService.cs` - 11 methods (Create, Update, Get, GetAll, GetActive, Deactivate, ExistsByCode, ExistsByName, etc.)
- `IGovernorateManagementService.cs` - 10 methods including GetSameDayDeliveryGovernorates
- `ICityManagementService.cs` - 11 methods including SearchCities, GetSameDayDeliveryCities

**Service Implementations:**
- `CountryManagementService.cs` - Full CRUD with uniqueness validation, logging
- `GovernorateManagementService.cs` - Parent validation, uniqueness within country
- `CityManagementService.cs` - Search functionality, hierarchical responses

**Key Features:**
- ✅ KISS principle (methods under 50 lines)
- ✅ Uniqueness validation (name/code within parent entity)
- ✅ Safe update pattern in repositories
- ✅ Comprehensive logging with Serilog
- ✅ Proper exception handling

### ✅ Phase 4: Update Existing Services (100%)

**StoreShippingConfigurationService.cs:**
- Added `ICityRepository` and `IStoreGovernorateSupportRepository` dependencies
- Updated Create/Update/Get methods to work with junction table
- **Completely rewrote `IsSameDayDeliveryAvailableAsync`** to use database-driven governorate support
- Removed all legacy city-based logic

**ShippingZoneService.cs:**
- Added `ICityRepository` and `IStoreGovernorateSupportRepository` dependencies
- Added production-ready `ResolveGovernorateFromCityAsync` method (database lookup)
- Added `IsGovernorateSupported` helper method
- Updated `DetermineStoreShippingZoneAsync` to check governorate support first

### ✅ Phase 5: Migration with Egyptian Seed Data (100%)

**Migration:** `20251006223445_AddLocationBasedShippingSystem.cs`

**Database Changes:**
- Created tables: Countries, Governorates, Cities, StoreGovernorateSupports
- Removed legacy fields: SupportedCities, ExcludedCities from StoreShippingConfigurations
- Composite unique index on StoreGovernorateSupports (StoreId, GovernorateId)

**Seed Data:**

**1 Country:**
- Egypt (ID: `11111111-1111-1111-1111-111111111111`, Code: "EG", NameArabic: "مصر")

**27 Egyptian Governorates:**
- **Greater Cairo:** Cairo (same-day enabled), Giza, Qalyubia
- **Delta:** Alexandria, Beheira, Gharbia, Kafr El Sheikh, Dakahlia, Damietta, Monufia, Sharqia
- **Canal:** Port Said, Ismailia, Suez
- **Sinai:** North Sinai, South Sinai
- **Upper Egypt:** Beni Suef, Fayoum, Minya, Asyut, Sohag, Qena, Luxor, Aswan
- **Western Desert:** Red Sea, New Valley, Matrouh

**20 Major Cities:**

**Cairo Cities (Same-Day Delivery Enabled):**
- Nasr City, Heliopolis, Maadi, Zamalek, Dokki, Mohandessin, New Cairo, Shorouk, Fifth Settlement, Rehab City

**Giza Cities:**
- 6th of October City, Sheikh Zayed, Haram, Faisal, Imbaba

**Alexandria Cities:**
- Miami, Sidi Gaber, Stanley, Smouha, Montaza

### ✅ Phase 6: DI Registration (100%)

**ConfigureServicesExtension.cs:**
```csharp
// Location Management Services
services.AddScoped<ICountryManagementService, CountryManagementService>();
services.AddScoped<IGovernorateManagementService, GovernorateManagementService>();
services.AddScoped<ICityManagementService, CityManagementService>();

// Location Repositories
services.AddScoped<ICountryRepository, CountryRepository>();
services.AddScoped<IGovernorateRepository, GovernorateRepository>();
services.AddScoped<ICityRepository, CityRepository>();
services.AddScoped<IStoreGovernorateSupportRepository, StoreGovernorateSupportRepository>();
```

### ⏳ Phase 7: API Controllers (Deferred)

**Planned Controllers:**
- `AdminCountriesController.cs` - [Authorize(Roles = "Admin")]
- `AdminGovernoratesController.cs` - [Authorize(Roles = "Admin")]
- `AdminCitiesController.cs` - [Authorize(Roles = "Admin")]
- `StoreShippingController.cs` - [Authorize(Roles = "Seller,Admin")]

**API endpoints will be implemented when needed.**

## Key Architectural Decisions

### 1. Junction Table vs Comma-Separated Strings
✅ **Chosen:** Junction table (`StoreGovernorateSupport`)
- **Benefits:** Referential integrity, better performance, proper foreign keys, cascade delete support
- **Pattern:** IsSupported flag distinguishes supported vs excluded governorates

### 2. City Resolution
✅ **Chosen:** Database-driven with `City` table
- **Benefits:** Production-ready, scalable, no hardcoded lists, easy to add new cities
- **Implementation:** `ResolveGovernorateFromCityAsync` queries database for city-to-governorate mapping

### 3. Same-Day Delivery Logic
✅ **Chosen:** Governorate-level with city-level override capability
- **Hierarchy:** Country → Governorate → City
- **Flexibility:** Cairo governorate enables same-day, individual Cairo cities can override
- **Database-Configurable:** No code changes needed to add/remove same-day cities

### 4. Safe Update Pattern
✅ **Chosen:** Explicit field updates in repositories
```csharp
// ✅ Safe: Only updates allowed fields
existingCountry.Name = country.Name;
existingCountry.NameArabic = country.NameArabic;
existingCountry.IsActive = country.IsActive;
existingCountry.UpdatedAt = DateTime.UtcNow;
// ❌ Does NOT update: CountryId, Code, CreatedAt

// ❌ Unsafe (avoided):
_context.Countries.Update(country); // Updates ALL fields
```

### 5. Legacy System Removal
✅ **Chosen:** Complete removal of city-based comma-separated strings
- **Removed:** SupportedCities, ExcludedCities from StoreShippingConfiguration
- **Reason:** Single source of truth, eliminates data conflicts, cleaner architecture

## Database Schema

```
Countries
├── CountryId (PK, Guid)
├── Name (nvarchar(100), Required)
├── Code (nvarchar(10), Required, Unique)
├── NameArabic (nvarchar(100))
├── IsActive (bit)
├── SupportsPostalCodes (bit)
├── CreatedAt / UpdatedAt
└── Navigation: Governorates (1:N)

Governorates
├── GovernorateId (PK, Guid)
├── CountryId (FK, Required)
├── Name (nvarchar(100), Required)
├── NameArabic (nvarchar(100))
├── Code (nvarchar(20))
├── IsActive (bit)
├── SupportsSameDayDelivery (bit)
├── CreatedAt / UpdatedAt
└── Navigation: Country (N:1), Cities (1:N)

Cities
├── CityId (PK, Guid)
├── GovernorateId (FK, Required)
├── Name (nvarchar(100), Required)
├── NameArabic (nvarchar(100))
├── Code (nvarchar(50))
├── IsActive (bit)
├── SupportsSameDayDelivery (bit)
├── CreatedAt / UpdatedAt
└── Navigation: Governorate (N:1)

StoreGovernorateSupports (Junction Table)
├── Id (PK, Guid)
├── StoreId (FK, Required)
├── GovernorateId (FK, Required)
├── IsSupported (bit) - true = supported, false = excluded
├── CreatedAt / UpdatedAt
├── Unique Index: (StoreId, GovernorateId)
└── Navigation: Store (N:1, Cascade), Governorate (N:1, Restrict)
```

## Store Owner Workflow

### 1. Configure Shipping Zones
```csharp
// Example: Cairo-only store
var request = new StoreShippingConfigurationRequest
{
    SupportedGovernorateIds = new List<Guid> { cairoId },
    SameDayDeliveryFee = 50.00m,
    StandardDeliveryFee = 30.00m,
    OffersSameDayDelivery = true,
    SameDayCutoffHour = 14
};

// Example: Greater Cairo store
var request = new StoreShippingConfigurationRequest
{
    SupportedGovernorateIds = new List<Guid> { cairoId, gizaId, qalyubiaId },
    StandardDeliveryFee = 40.00m,
    NationalDeliveryFee = 60.00m,
    OffersSameDayDelivery = false
};

// Example: Nationwide with exclusions
var request = new StoreShippingConfigurationRequest
{
    ExcludedGovernorateIds = new List<Guid> { northSinaiId, southSinaiId },
    StandardDeliveryFee = 50.00m,
    NationalDeliveryFee = 80.00m
};
```

### 2. Same-Day Delivery Validation
```csharp
// System checks:
// 1. Store configuration: OffersSameDayDelivery = true
// 2. Database lookup: City → Governorate
// 3. Junction table: Store supports this governorate
// 4. Governorate setting: SupportsSameDayDelivery = true
// 5. City override: City.SupportsSameDayDelivery (optional)
// 6. Cutoff time: Current hour < SameDayCutoffHour

var isAvailable = await _shippingService.IsSameDayDeliveryAvailableAsync(storeId, "Nasr City");
// Returns: true (Cairo governorate + Cairo city both enabled)
```

## Benefits

### For Admins
✅ Easy expansion to new countries (Saudi Arabia, UAE, etc.)
✅ No code changes needed to add locations
✅ Centralized location management
✅ Support for localization (Arabic names)

### For Store Owners
✅ Visual governorate selection (when UI implemented)
✅ Easy configuration updates
✅ Clear shipping coverage
✅ Flexible whitelist/blacklist approach

### For Developers
✅ No hardcoded city lists
✅ Scalable architecture
✅ Easy maintenance
✅ Clean separation of concerns
✅ KISS principle throughout

## Testing (When Needed)

### Unit Tests
```csharp
[Test]
public async Task IsSameDayDeliveryAvailable_CairoCity_ReturnsTrue()
{
    // Arrange
    var storeId = Guid.NewGuid();
    // Act
    var isAvailable = await _shippingService.IsSameDayDeliveryAvailableAsync(storeId, "Nasr City");
    // Assert
    Assert.IsTrue(isAvailable);
}
```

### Integration Tests
- Test store can configure supported governorates
- Test same-day delivery validation with database
- Test city resolution via database lookup
- Test order calculation uses correct delivery fees

## Future Expansion

**To add Saudi Arabia:**
1. Admin creates Saudi Arabia country via service (when API implemented)
2. Admin adds 13 Saudi regions (Riyadh, Makkah, etc.)
3. Admin adds major cities (Riyadh, Jeddah, Dammam)
4. Store owners select Saudi regions to ship to
5. **No code changes required**

---

# 4. API Usage Examples

## Overview
This section provides examples of common API usage patterns in the Bazario platform.

## User Registration with Role Selection

Users can choose to register as either a **Customer** or **Seller** during registration.

### Customer Registration Example
```json
POST /api/auth/register

{
  "email": "customer@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "role": "Customer",
  "gender": "Male",
  "age": 25,
  "phoneNumber": "+1234567890"
}
```

### Seller Registration Example
```json
POST /api/auth/register

{
  "email": "seller@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "role": "Seller",
  "gender": "Female",
  "age": 30,
  "phoneNumber": "+1234567890"
}
```

## What Happens During Registration

1. **Role Validation**: System validates that the role is either "Customer" or "Seller"
2. **Role Creation**: If the role doesn't exist, it's automatically created
3. **User Creation**: User account is created with the selected role
4. **Token Generation**: JWT tokens are generated with the user's role
5. **Response**: Success message includes the assigned role

## Response Examples

### Success Response
```json
{
  "isSuccess": true,
  "message": "User registered successfully as customer.",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh_token_here",
  "accessTokenExpiration": "2024-01-15T10:30:00Z",
  "refreshTokenExpiration": "2024-01-22T10:30:00Z",
  "user": {
    "id": "user-guid-here",
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["customer"],
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### Error Response (Invalid Role)
```json
{
  "isSuccess": false,
  "message": "Invalid role. Role must be either 'Customer' or 'Seller'.",
  "errors": []
}
```

## Benefits

✅ **User Choice**: Users can choose their role during registration
✅ **Automatic Role Management**: Roles are created automatically if they don't exist
✅ **Consistent Validation**: Role names are normalized for consistency
✅ **Clear Feedback**: Success messages include the assigned role
✅ **Security**: Only valid roles (Customer/Seller) are accepted

## Frontend Implementation Example

```javascript
// Example frontend form
const registrationForm = {
  email: "user@example.com",
  firstName: "John",
  lastName: "Doe",
  password: "SecurePassword123!",
  confirmPassword: "SecurePassword123!",
  role: "Customer", // or "Seller"
  gender: "Male",
  age: 25,
  phoneNumber: "+1234567890"
};

// Send registration request
const response = await fetch('/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(registrationForm)
});

const data = await response.json();
if (data.isSuccess) {
  // Store tokens
  localStorage.setItem('accessToken', data.accessToken);
  localStorage.setItem('refreshToken', data.refreshToken);

  // Redirect based on role
  if (data.user.roles.includes('seller')) {
    window.location.href = '/seller-dashboard';
  } else {
    window.location.href = '/customer-dashboard';
  }
}
```

---

## Additional Resources

- **TODO.md** - Development roadmap and task tracking
- **Bazario_PRD.md** - Product requirements and business objectives
- **Services/README.md** - Service architecture and best practices
- **Swagger/OpenAPI** - Complete API documentation (when controllers are implemented)

---

*This is a living document. Please update it when configurations or procedures change.*