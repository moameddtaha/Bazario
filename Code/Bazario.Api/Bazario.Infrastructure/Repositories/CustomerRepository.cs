using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public CustomerRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<ApplicationUser> AddCustomerAsync(ApplicationUser customer, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));

                // Create "Customer" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    var customerRole = new ApplicationRole { Name = "Customer" };
                    var roleResult = await _roleManager.CreateAsync(customerRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create Customer role: {roleErrors}");
                    }
                }

                // Create the customer user using UserManager with password
                var result = await _userManager.CreateAsync(customer, password);
                
                if (result.Succeeded)
                {
                    // Add customer role
                    var roleAssignResult = await _userManager.AddToRoleAsync(customer, "Customer");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        await _userManager.DeleteAsync(customer);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to assign Customer role: {roleErrors}");
                    }
                    
                    return customer;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create customer: {errors}");
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while creating customer: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    return false; // Invalid ID
                }
                
                // Find the customer user by ID
                var customer = await _userManager.FindByIdAsync(customerId.ToString());
                
                if (customer == null)
                {
                    return false; // Customer not found
                }
                
                // Check if user is actually in Customer role
                var isCustomer = await _userManager.IsInRoleAsync(customer, "Customer");
                if (!isCustomer)
                {
                    throw new InvalidOperationException("User is not a customer");
                }
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                // Delete the customer user
                var result = await _userManager.DeleteAsync(customer);
                
                if (result.Succeeded)
                {
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete customer: {errors}");
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting customer with ID {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllCustomersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Customer role exists
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    // Return empty list if Customer role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                // Get all users in Customer role
                var customers = await _userManager.GetUsersInRoleAsync("Customer");
                
                // Convert to List and return (handles null case)
                return customers?.ToList() ?? new List<ApplicationUser>();
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve customers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customerId == Guid.Empty)
                {
                    return null; // Invalid ID
                }
                
                // Find the customer user by ID
                var customer = await _userManager.FindByIdAsync(customerId.ToString());
                
                if (customer == null)
                {
                    return null; // Customer not found
                }
                
                // Check if Customer role exists before checking user role
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    return null; // Customer role doesn't exist, so no customers possible
                }
                
                // Check if user is actually in Customer role
                var isCustomer = await _userManager.IsInRoleAsync(customer, "Customer");
                if (!isCustomer)
                {
                    return null; // User exists but is not a customer
                }
                
                return customer;
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve customer with ID {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetFilteredCustomersAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                // Check if Customer role exists
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    // Return empty list if Customer role doesn't exist
                    return new List<ApplicationUser>();
                }

                // Get all customers and filter by predicate
                var allCustomers = await _userManager.GetUsersInRoleAsync("Customer");
                var filteredCustomers = allCustomers.AsQueryable().Where(predicate.Compile()).ToList();

                return filteredCustomers;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered customers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateCustomerAsync(ApplicationUser customer, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));
                
                if (customer.Id == Guid.Empty)
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customer));

                // Check if customer exists and is actually a customer
                var existingCustomer = await GetCustomerByIdAsync(customer.Id, cancellationToken);
                if (existingCustomer == null)
                {
                    throw new InvalidOperationException($"Customer with ID {customer.Id} not found or is not a customer");
                }

                // Update the customer user using UserManager
                var result = await _userManager.UpdateAsync(customer);
                
                if (result.Succeeded)
                {
                    // Ensure the customer still has the Customer role (in case it was somehow removed)
                    var isInCustomerRole = await _userManager.IsInRoleAsync(customer, "Customer");
                    if (!isInCustomerRole)
                    {
                        await _userManager.AddToRoleAsync(customer, "Customer");
                    }
                    
                    return customer;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update customer: {errors}");
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while updating customer with ID {customer?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeCustomerPasswordAsync(Guid customerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (customerId == Guid.Empty)
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
                if (string.IsNullOrWhiteSpace(currentPassword))
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get customer and verify role
                var customer = await GetCustomerByIdAsync(customerId, cancellationToken);
                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer with ID {customerId} not found or is not a customer");
                }

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(customer, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to change customer password: {errors}");
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while changing customer password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetCustomerPasswordAsync(Guid customerId, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (customerId == Guid.Empty)
                    throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get customer and verify role
                var customer = await GetCustomerByIdAsync(customerId, cancellationToken);
                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer with ID {customerId} not found or is not a customer");
                }

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(customer);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(customer, newPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to set new password: {errors}");
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while resetting customer password: {ex.Message}", ex);
            }
        }
    }
}
