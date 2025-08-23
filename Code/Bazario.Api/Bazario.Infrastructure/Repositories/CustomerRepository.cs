using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<CustomerRepository> _logger;

        public CustomerRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger<CustomerRepository> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationUser> AddCustomerAsync(ApplicationUser customer, string password, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new customer user: {CustomerId}", customer?.Id);
            
            try
            {
                // Validate inputs
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to add null customer user");
                    throw new ArgumentNullException(nameof(customer));
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Attempted to add customer with null or empty password");
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                _logger.LogDebug("Creating Customer role if it doesn't exist");

                // Create "Customer" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    var customerRole = new ApplicationRole { Name = "Customer" };
                    var roleResult = await _roleManager.CreateAsync(customerRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create Customer role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to create Customer role: {roleErrors}");
                    }
                    _logger.LogDebug("Customer role created successfully");
                }
                else
                {
                    _logger.LogDebug("Customer role already exists");
                }

                _logger.LogDebug("Creating customer user with UserManager. Email: {Email}, UserName: {UserName}", 
                    customer.Email, customer.UserName);

                // Create the customer user using UserManager with password
                var result = await _userManager.CreateAsync(customer, password);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Customer user created successfully, assigning Customer role");

                    // Add customer role
                    var roleAssignResult = await _userManager.AddToRoleAsync(customer, "Customer");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        _logger.LogWarning("Role assignment failed, deleting created user to maintain consistency");
                        await _userManager.DeleteAsync(customer);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to assign Customer role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to assign Customer role: {roleErrors}");
                    }
                    
                    _logger.LogInformation("Successfully added customer user. CustomerId: {CustomerId}, Email: {Email}", 
                        customer.Id, customer.Email);
                    return customer;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create customer user: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create customer: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding customer user: {CustomerId}", customer?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while adding customer user: {CustomerId}", customer?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating customer user: {CustomerId}", customer?.Id);
                throw new InvalidOperationException($"Unexpected error while creating customer: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete customer user: {CustomerId}", customerId);
            
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete customer with empty ID");
                    return false; // Invalid ID
                }
                
                _logger.LogDebug("Finding customer user by ID: {CustomerId}", customerId);
                
                // Find the customer user by ID
                var customer = await _userManager.FindByIdAsync(customerId.ToString());
                
                if (customer == null)
                {
                    _logger.LogWarning("Customer user not found for deletion. CustomerId: {CustomerId}", customerId);
                    return false; // Customer not found
                }
                
                _logger.LogDebug("Verifying customer role for user: {CustomerId}", customerId);
                
                // Check if user is actually in Customer role
                var isCustomer = await _userManager.IsInRoleAsync(customer, "Customer");
                if (!isCustomer)
                {
                    _logger.LogWarning("User is not a customer. CustomerId: {CustomerId}", customerId);
                    throw new InvalidOperationException("User is not a customer");
                }
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                _logger.LogDebug("Proceeding with customer deletion. CustomerId: {CustomerId}", customerId);
                
                // Delete the customer user
                var result = await _userManager.DeleteAsync(customer);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted customer user. CustomerId: {CustomerId}, Email: {Email}", 
                        customerId, customer.Email);
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete customer user: {Errors}. CustomerId: {CustomerId}", errors, customerId);
                throw new InvalidOperationException($"Failed to delete customer: {errors}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while deleting customer: {CustomerId}", customerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Unexpected error while deleting customer with ID {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllCustomersAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve all customer users");
            
            try
            {
                _logger.LogDebug("Checking if Customer role exists");
                
                // Check if Customer role exists
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    _logger.LogDebug("Customer role doesn't exist, returning empty list");
                    // Return empty list if Customer role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                _logger.LogDebug("Getting all users in Customer role");
                
                // Get all users in Customer role
                var customers = await _userManager.GetUsersInRoleAsync("Customer");
                
                // Convert to List and return (handles null case)
                var customerList = customers?.ToList() ?? new List<ApplicationUser>();
                
                _logger.LogDebug("Successfully retrieved {CustomerCount} customer users", customerList.Count);
                
                return customerList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all customer users");
                throw new InvalidOperationException($"Failed to retrieve customers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve customer by ID: {CustomerId}", customerId);
            
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve customer with empty ID");
                    return null; // Invalid ID
                }
                
                _logger.LogDebug("Finding customer user by ID: {CustomerId}", customerId);
                
                // Find the customer user by ID
                var customer = await _userManager.FindByIdAsync(customerId.ToString());
                
                if (customer == null)
                {
                    _logger.LogDebug("Customer user not found. CustomerId: {CustomerId}", customerId);
                    return null; // Customer not found
                }
                
                _logger.LogDebug("Checking if Customer role exists");
                
                // Check if Customer role exists before checking user role
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    _logger.LogDebug("Customer role doesn't exist, no customers possible. CustomerId: {CustomerId}", customerId);
                    return null; // Customer role doesn't exist, so no customers possible
                }
                
                _logger.LogDebug("Verifying customer role for user: {CustomerId}", customerId);
                
                // Check if user is actually in Customer role
                var isCustomer = await _userManager.IsInRoleAsync(customer, "Customer");
                if (!isCustomer)
                {
                    _logger.LogDebug("User exists but is not a customer. CustomerId: {CustomerId}", customerId);
                    return null; // User exists but is not a customer
                }
                
                _logger.LogDebug("Successfully retrieved customer user. CustomerId: {CustomerId}, Email: {Email}", 
                    customerId, customer.Email);
                
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to retrieve customer with ID {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetFilteredCustomersAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered customer users");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve customers with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Checking if Customer role exists");

                // Check if Customer role exists
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    _logger.LogDebug("Customer role doesn't exist, returning empty list");
                    // Return empty list if Customer role doesn't exist
                    return new List<ApplicationUser>();
                }

                _logger.LogDebug("Getting all customers and applying filter");

                // Get all customers and filter by predicate
                var allCustomers = await _userManager.GetUsersInRoleAsync("Customer");
                var filteredCustomers = allCustomers.AsQueryable().Where(predicate.Compile()).ToList();

                _logger.LogDebug("Successfully retrieved {FilteredCustomerCount} filtered customer users from {TotalCustomerCount} total", 
                    filteredCustomers.Count, allCustomers.Count);

                return filteredCustomers;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered customer users");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered customer users");
                throw new InvalidOperationException($"Failed to retrieve filtered customers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateCustomerAsync(ApplicationUser customer, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update customer user: {CustomerId}", customer?.Id);
            
            try
            {
                // Validate input
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to update null customer user");
                    throw new ArgumentNullException(nameof(customer));
                }
                
                if (customer.Id == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update customer with empty ID");
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customer));
                }

                _logger.LogDebug("Verifying customer exists and has customer role. CustomerId: {CustomerId}", customer.Id);

                // Check if customer exists and is actually a customer
                var existingCustomer = await GetCustomerByIdAsync(customer.Id, cancellationToken);
                if (existingCustomer == null)
                {
                    _logger.LogWarning("Customer not found or is not a customer. CustomerId: {CustomerId}", customer.Id);
                    throw new InvalidOperationException($"Customer with ID {customer.Id} not found or is not a customer");
                }

                _logger.LogDebug("Updating customer user with UserManager. CustomerId: {CustomerId}", customer.Id);

                // Update the customer user using UserManager
                var result = await _userManager.UpdateAsync(customer);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Customer user updated successfully, ensuring Customer role is maintained");

                    // Ensure the customer still has the Customer role (in case it was somehow removed)
                    var isInCustomerRole = await _userManager.IsInRoleAsync(customer, "Customer");
                    if (!isInCustomerRole)
                    {
                        _logger.LogDebug("Customer role was missing, reassigning it. CustomerId: {CustomerId}", customer.Id);
                        await _userManager.AddToRoleAsync(customer, "Customer");
                    }
                    
                    _logger.LogInformation("Successfully updated customer user. CustomerId: {CustomerId}, Email: {Email}", 
                        customer.Id, customer.Email);
                    return customer;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update customer user: {Errors}. CustomerId: {CustomerId}", errors, customer.Id);
                throw new InvalidOperationException($"Failed to update customer: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating customer user: {CustomerId}", customer?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating customer user: {CustomerId}", customer?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating customer user: {CustomerId}", customer?.Id);
                throw new InvalidOperationException($"Unexpected error while updating customer with ID {customer?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeCustomerPasswordAsync(Guid customerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to change customer password: {CustomerId}", customerId);
            
            try
            {
                // Validate inputs
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to change password with empty customer ID");
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
                }
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty current password. CustomerId: {CustomerId}", customerId);
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty new password. CustomerId: {CustomerId}", customerId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting customer and verifying role. CustomerId: {CustomerId}", customerId);

                // Get customer and verify role
                var customer = await GetCustomerByIdAsync(customerId, cancellationToken);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found or is not a customer. CustomerId: {CustomerId}", customerId);
                    throw new InvalidOperationException($"Customer with ID {customerId} not found or is not a customer");
                }

                _logger.LogDebug("Changing password using UserManager. CustomerId: {CustomerId}", customerId);

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(customer, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to change customer password: {Errors}. CustomerId: {CustomerId}", errors, customerId);
                    throw new InvalidOperationException($"Failed to change customer password: {errors}");
                }

                _logger.LogInformation("Successfully changed customer password. CustomerId: {CustomerId}", customerId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while changing customer password: {CustomerId}", customerId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while changing customer password: {CustomerId}", customerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while changing customer password: {CustomerId}", customerId);
                throw new InvalidOperationException($"Unexpected error while changing customer password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetCustomerPasswordAsync(Guid customerId, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to reset customer password: {CustomerId}", customerId);
            
            try
            {
                // Validate inputs
                if (customerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to reset password with empty customer ID");
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to reset password with empty new password. CustomerId: {CustomerId}", customerId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting customer and verifying role. CustomerId: {CustomerId}", customerId);

                // Get customer and verify role
                var customer = await GetCustomerByIdAsync(customerId, cancellationToken);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found or is not a customer. CustomerId: {CustomerId}", customerId);
                    throw new InvalidOperationException($"Customer with ID {customerId} not found or is not a customer");
                }

                _logger.LogDebug("Removing current password. CustomerId: {CustomerId}", customerId);

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(customer);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to remove current password: {Errors}. CustomerId: {CustomerId}", errors, customerId);
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                _logger.LogDebug("Adding new password. CustomerId: {CustomerId}", customerId);

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(customer, newPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to set new password: {Errors}. CustomerId: {CustomerId}", errors, customerId);
                    throw new InvalidOperationException($"Failed to set new password: {errors}");
                }

                _logger.LogInformation("Successfully reset customer password. CustomerId: {CustomerId}", customerId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while resetting customer password: {CustomerId}", customerId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while resetting customer password: {CustomerId}", customerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while resetting customer password: {CustomerId}", customerId);
                throw new InvalidOperationException($"Unexpected error while resetting customer password: {ex.Message}", ex);
            }
        }
    }
}
