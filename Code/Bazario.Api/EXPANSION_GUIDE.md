# Shipping Zone Service Expansion Guide

## Overview
The shipping zone service is designed to be Egypt-focused but easily expandable to other countries. This guide explains how to add support for new countries.

## Current Status
- **Primary Market**: Egypt (EG, EGYPT, EGY)
- **Postal Code Support**: Not used (Egypt doesn't use postal codes for shipping)
- **Delivery Types**: Same-day (Cairo/Alexandria), Express (major cities), Local, Regional, National, Remote

## Adding a New Country

### 1. Update Country Support
In `ShippingZoneService.cs`, add the new country to the `IsCountrySupported` method:

```csharp
private bool IsCountrySupported(string country)
{
    var supportedCountries = new HashSet<string>
    {
        "EG", "EGYPT", "EGY",  // Egypt (current)
        "US", "CA", "GB", "UK" // New countries
    };
    return supportedCountries.Contains(countryUpper);
}
```

### 2. Add Postal Code Support (if applicable)
If the new country uses postal codes for shipping zones, add it to `IsPostalCodeSupported`:

```csharp
private bool IsPostalCodeSupported(string country)
{
    var postalCodeCountries = new HashSet<string>
    {
        "US", "CA", "GB", "UK", "DE", "FR" // Countries using postal codes
    };
    return postalCodeCountries.Contains(countryUpper);
}
```

### 3. Add City Lists
Create new methods for express and same-day delivery cities:

```csharp
private HashSet<string> GetUSExpressDeliveryCities()
{
    return new HashSet<string>
    {
        "NEW YORK", "LOS ANGELES", "CHICAGO", "HOUSTON", "PHOENIX",
        "PHILADELPHIA", "SAN ANTONIO", "SAN DIEGO", "DALLAS", "SAN JOSE"
    };
}

private HashSet<string> GetUSSameDayDeliveryCities()
{
    return new HashSet<string>
    {
        "NEW YORK", "LOS ANGELES", "CHICAGO"
    };
}
```

### 4. Update Country-Specific Methods
Update the switch statements in `GetExpressDeliveryCitiesForCountry` and `GetSameDayDeliveryCitiesForCountry`:

```csharp
return countryUpper switch
{
    "EG" or "EGYPT" or "EGY" => GetEgyptianExpressDeliveryCities(),
    "US" => GetUSExpressDeliveryCities(),
    "CA" => GetCanadaExpressDeliveryCities(),
    "GB" or "UK" => GetUKExpressDeliveryCities(),
    _ => new HashSet<string>()
};
```

### 5. Update Default Zones
Add the new country to `GetDefaultZoneForCountry`:

```csharp
return country?.ToUpperInvariant() switch
{
    "EG" or "EGYPT" or "EGY" => ShippingZone.Local,
    "US" or "CA" => ShippingZone.National,
    "GB" or "UK" or "DE" or "FR" or "IT" or "ES" => ShippingZone.International,
    _ => ShippingZone.Local
};
```

### 6. Update Configuration
Add the new country's zones to `appsettings.json`:

```json
{
  "Shipping": {
    "PostalCodeZones": {
      "10001": "SameDay",
      "10002": "SameDay",
      "90210": "Express"
    },
    "CityZones": {
      "NEW YORK": "SameDay",
      "LOS ANGELES": "SameDay",
      "CHICAGO": "Express"
    },
    "StateZones": {
      "NY": "Regional",
      "CA": "Regional",
      "IL": "Regional"
    },
    "CountryZones": {
      "US": "National",
      "CA": "National"
    }
  }
}
```

## Example: Adding United States Support

### Step 1: Add US to supported countries
```csharp
var supportedCountries = new HashSet<string>
{
    "EG", "EGYPT", "EGY",  // Egypt
    "US"                   // United States
};
```

### Step 2: Add postal code support
```csharp
var postalCodeCountries = new HashSet<string>
{
    "US"  // US uses postal codes
};
```

### Step 3: Create US city lists
```csharp
private HashSet<string> GetUSExpressDeliveryCities()
{
    return new HashSet<string>
    {
        "NEW YORK", "LOS ANGELES", "CHICAGO", "HOUSTON", "PHOENIX",
        "PHILADELPHIA", "SAN ANTONIO", "SAN DIEGO", "DALLAS", "SAN JOSE",
        "AUSTIN", "JACKSONVILLE", "FORT WORTH", "COLUMBUS", "CHARLOTTE",
        "SAN FRANCISCO", "INDIANAPOLIS", "SEATTLE", "DENVER", "BOSTON"
    };
}

private HashSet<string> GetUSSameDayDeliveryCities()
{
    return new HashSet<string>
    {
        "NEW YORK", "LOS ANGELES", "CHICAGO", "HOUSTON", "PHOENIX"
    };
}
```

### Step 4: Update configuration
```json
{
  "Shipping": {
    "PostalCodeZones": {
      "10001": "SameDay",
      "10002": "SameDay",
      "10003": "SameDay",
      "90210": "SameDay",
      "90211": "SameDay",
      "60601": "SameDay",
      "60602": "SameDay"
    },
    "CityZones": {
      "NEW YORK": "SameDay",
      "LOS ANGELES": "SameDay",
      "CHICAGO": "SameDay",
      "HOUSTON": "Express",
      "PHOENIX": "Express",
      "PHILADELPHIA": "Express",
      "SAN ANTONIO": "Express",
      "SAN DIEGO": "Express",
      "DALLAS": "Express",
      "SAN JOSE": "Express"
    },
    "StateZones": {
      "NY": "Regional",
      "CA": "Regional",
      "IL": "Regional",
      "TX": "Regional",
      "FL": "Regional",
      "PA": "Regional",
      "OH": "Regional",
      "GA": "Regional",
      "NC": "Regional",
      "MI": "Regional"
    },
    "CountryZones": {
      "US": "National"
    }
  }
}
```

## Testing New Countries

### 1. Unit Tests
Create unit tests for the new country's shipping zones:

```csharp
[Test]
public async Task DetermineShippingZoneAsync_USAddress_ReturnsCorrectZone()
{
    // Arrange
    var address = "123 Main St";
    var city = "New York";
    var state = "NY";
    var country = "US";
    var postalCode = "10001";

    // Act
    var result = await _shippingZoneService.DetermineShippingZoneAsync(
        address, city, state, country, postalCode);

    // Assert
    Assert.AreEqual(ShippingZone.SameDay, result);
}
```

### 2. Integration Tests
Test the full shipping zone determination flow with real addresses.

### 3. Configuration Tests
Verify that the configuration is loaded correctly for the new country.

## Best Practices

1. **Start Small**: Begin with major cities and expand gradually
2. **Use Configuration**: Store zone mappings in configuration files, not hardcoded
3. **Log Everything**: Add comprehensive logging for debugging
4. **Test Thoroughly**: Create unit and integration tests for new countries
5. **Document Changes**: Update this guide when adding new countries
6. **Consider Localization**: Some countries may have different address formats
7. **Performance**: Consider caching for frequently accessed zone data

## Future Enhancements

1. **Database Integration**: Move zone data to a database for easier management
2. **External APIs**: Integrate with shipping provider APIs for real-time zone data
3. **Machine Learning**: Use ML to predict optimal shipping zones
4. **Real-time Updates**: Update zones based on current shipping conditions
5. **Multi-language Support**: Support different languages for city/state names

## Maintenance

- Regularly review and update city lists as new areas are added
- Monitor shipping performance and adjust zones accordingly
- Keep configuration files synchronized across environments
- Update documentation when making changes
