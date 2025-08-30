# User Registration with Role Selection

## Overview
Users can now choose to register as either a **Customer** or **Seller** during registration.

## Registration Request Example

### Customer Registration
```json
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

### Seller Registration
```json
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

## Frontend Implementation

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
```
