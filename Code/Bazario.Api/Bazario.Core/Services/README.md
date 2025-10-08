# Bazario Services Architecture Guide

This document explains the service architecture and provides guidance on which services to use and when.

## Architecture Overview

Bazario follows a **layered service architecture** with clear separation of concerns:

```
Controllers → Composite Services → Specialized Services → Repositories
                    ↓
                  Helpers
```

## Service Types

### 1. Composite Services (Facade Pattern)
**Examples:** `OrderService`, `ProductService`, `StoreService`, `InventoryService`

**Purpose:** Provide a unified interface that delegates to specialized services.

**When to use:**
- ✅ When you need multiple operations across different concerns (query + validation + management)
- ✅ In controllers when you want a single dependency instead of multiple
- ✅ For backward compatibility when refactoring

**When NOT to use:**
- ❌ When you only need one specific operation (use specialized service instead)
- ❌ In new code where you can inject specific services directly

**Example:**
```csharp
// Using composite service (acceptable)
public class OrderController
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }
}

// Using specialized services (preferred for new code)
public class OrderController
{
    private readonly IOrderManagementService _orderManagement;
    private readonly IOrderQueryService _orderQuery;

    public OrderController(
        IOrderManagementService orderManagement,
        IOrderQueryService orderQuery)
    {
        _orderManagement = orderManagement;
        _orderQuery = orderQuery;
    }
}
```

### 2. Specialized Services (Single Responsibility)
**Examples:** `OrderManagementService`, `OrderQueryService`, `OrderValidationService`

**Purpose:** Each service focuses on ONE aspect of the domain.

**Types:**
- **Management Services** (`*ManagementService`) - CRUD operations
- **Query Services** (`*QueryService`) - Read operations and searches
- **Validation Services** (`*ValidationService`) - Business rules and validations
- **Analytics Services** (`*AnalyticsService`) - Reports and metrics
- **Payment Services** (`*PaymentService`) - Payment processing

**When to use:**
- ✅ **Always prefer these over composite services in new code**
- ✅ When you need focused, testable components
- ✅ When building new features or refactoring

**Example:**
```csharp
// ✅ GOOD: Clear, focused dependencies
public class OrderProcessingService
{
    private readonly IOrderManagementService _orderManagement;
    private readonly IOrderValidationService _orderValidation;

    public async Task<Order> CreateValidatedOrder(OrderRequest request)
    {
        // Validate first with detailed feedback
        var stockValidation = await _orderValidation.ValidateStockAvailabilityWithDetailsAsync(request.Items);

        if (!stockValidation.IsValid)
        {
            throw new InvalidOperationException($"Stock validation failed: {stockValidation.Message}");
        }

        // Then create
        return await _orderManagement.CreateOrderAsync(request);
    }
}

// ❌ BAD: Using composite when specialized would be clearer
public class OrderProcessingService
{
    private readonly IOrderService _orderService;

    // Less clear which operations are being used
}
```

### 3. Helper Classes
**Examples:** `OrderCalculator`, `TokenHelper`, `EmailHelper`

**Purpose:** Extract complex calculations or algorithms to keep services simple.

**When to create a helper:**
- ✅ When a method exceeds ~50 lines
- ✅ When logic is reusable across multiple services
- ✅ When the logic is complex but doesn't fit into a service category
- ✅ When following KISS (Keep It Simple, Stupid) principle

**Example:**
```csharp
// ✅ GOOD: Complex calculation extracted to helper
public class OrderValidationService
{
    private readonly OrderCalculator _calculator;

    public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(...)
    {
        // Simple, readable steps
        var subtotal = await _calculator.CalculateSubtotalAsync(items);
        var shipping = await _calculator.CalculateShippingCostAsync(stores, address);
        var discounts = await _calculator.CalculateDiscountsAsync(codes, subtotal);

        return new OrderTotalCalculation
        {
            Subtotal = subtotal,
            Shipping = shipping,
            Discount = discounts,
            Total = subtotal + shipping - discounts
        };
    }
}

// ❌ BAD: 200+ line method doing everything inline
public class OrderValidationService
{
    public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(...)
    {
        // 200 lines of complex calculation logic...
        // Hard to test, hard to maintain
    }
}
```

## KISS Principle Applied

**Keep It Simple, Stupid** - our refactoring philosophy:

### Before Refactoring (Complex)
```csharp
// ❌ Complex: 200-line method, multiple responsibilities
public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(...)
{
    // Validate inputs
    // Calculate subtotal
    // Group by store
    // Calculate shipping for each store
    // Handle fallback shipping
    // Calculate multiple discounts
    // Validate discount rules
    // Calculate total
    // 200+ lines of intertwined logic
}
```

### After Refactoring (Simple)
```csharp
// ✅ Simple: Clear steps, delegated complexity
public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(...)
{
    ValidateCalculationInputs(orderItems, shippingAddress);

    var subtotal = await _calculator.CalculateSubtotalAsync(orderItems);
    var itemsByStore = await _calculator.GroupItemsByStoreAsync(orderItems);
    var shipping = await _calculator.CalculateShippingCostAsync(itemsByStore, shippingAddress);
    var (discount, appliedDiscounts, _) = await _calculator.CalculateDiscountsAsync(
        discountCodes, subtotal, storeIds);

    return new OrderTotalCalculation
    {
        Subtotal = subtotal,
        ShippingCost = shipping,
        DiscountAmount = discount,
        Total = subtotal + shipping - discount,
        AppliedDiscounts = appliedDiscounts
    };
}
```

**Benefits:**
- Each step is a focused method (< 50 lines)
- Easy to test each calculation independently
- Clear what the method does at a glance
- Easy to modify one aspect without touching others

## Service Selection Decision Tree

```
Need to work with orders?
│
├─ Need CRUD operations only?
│  └─ Use IOrderManagementService
│
├─ Need read-only queries?
│  └─ Use IOrderQueryService
│
├─ Need validation or calculations?
│  └─ Use IOrderValidationService
│
├─ Need analytics/reports?
│  └─ Use IOrderAnalyticsService
│
├─ Need payment processing?
│  └─ Use IOrderPaymentService
│
└─ Need multiple operations across services?
   ├─ New code? Inject specific services needed
   └─ Legacy code? Can use IOrderService (composite)
```

## Best Practices

### ✅ DO

1. **Inject only what you need**
   ```csharp
   // Good: Only inject services you'll use
   public class OrderReportGenerator
   {
       private readonly IOrderAnalyticsService _analytics;
   }
   ```

2. **Keep methods focused and small (< 50 lines)**
   ```csharp
   // Good: Small, focused method
   public async Task<decimal> CalculateSubtotal(List<OrderItem> items)
   {
       decimal total = 0;
       foreach (var item in items)
       {
           var product = await _productRepo.GetByIdAsync(item.ProductId);
           total += product.Price * item.Quantity;
       }
       return total;
   }
   ```

3. **Extract complex logic to helpers**
   ```csharp
   // Good: Complex logic in helper
   var shipping = await _shippingCalculator.CalculateShippingCostAsync(...);
   ```

4. **Use validation services for business rules**
   ```csharp
   // Good: Validation in validation service
   _orderValidation.ValidateOrderUpdateBusinessRules(request, existingOrder);
   ```

### ❌ DON'T

1. **Don't inject composite services when specialized will do**
   ```csharp
   // Bad: Injecting composite when only need queries
   public class OrderReportGenerator
   {
       private readonly IOrderService _orderService; // Overkill!
   }
   ```

2. **Don't write methods over 50 lines**
   ```csharp
   // Bad: 200-line method
   public async Task<OrderCalculation> CalculateEverything(...)
   {
       // 200 lines of logic
   }
   ```

3. **Don't put validation logic in management services**
   ```csharp
   // Bad: Validation logic in management service
   public class OrderManagementService
   {
       public async Task UpdateOrder(...)
       {
           // Validation logic here (belongs in OrderValidationService)
           if (discount > subtotal) throw new Exception(...);
       }
   }
   ```

## Migration Guide

If you have old code using composite services, here's how to migrate:

### Old Code (Composite)
```csharp
public class OrderController
{
    private readonly IOrderService _orderService;

    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return Ok(order);
    }

    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        var order = await _orderService.CreateOrderAsync(request);
        return Ok(order);
    }
}
```

### New Code (Specialized)
```csharp
public class OrderController
{
    private readonly IOrderQueryService _orderQuery;
    private readonly IOrderManagementService _orderManagement;

    public OrderController(
        IOrderQueryService orderQuery,
        IOrderManagementService orderManagement)
    {
        _orderQuery = orderQuery;
        _orderManagement = orderManagement;
    }

    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderQuery.GetOrderByIdAsync(id);
        return Ok(order);
    }

    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        var order = await _orderManagement.CreateOrderAsync(request);
        return Ok(order);
    }
}
```

**Benefits of migration:**
- Clearer dependencies (obvious what operations are needed)
- Easier to test (mock only what's used)
- Follows SOLID principles (Interface Segregation)
- Better performance (no unnecessary service initialization)

## Summary

1. **Prefer specialized services** (`IOrderManagementService`, `IOrderQueryService`, etc.)
2. **Use helpers** for complex calculations (`OrderCalculator`, etc.)
3. **Keep methods simple** (< 50 lines, single responsibility)
4. **Follow KISS** - if it's getting complex, extract it
5. **Composite services** are fine for backward compatibility but avoid in new code

For questions or suggestions, please contact the development team.
