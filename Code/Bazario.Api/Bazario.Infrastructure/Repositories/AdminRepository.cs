using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AdminRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<ApplicationUser> AddAdminAsync(ApplicationUser admin, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (admin == null)
                    throw new ArgumentNullException(nameof(admin));
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));

                // Create "Admin" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var adminRole = new ApplicationRole { Name = "Admin" };
                    var roleResult = await _roleManager.CreateAsync(adminRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create Admin role: {roleErrors}");
                    }
                }

                // Create the admin user using UserManager with password
                var result = await _userManager.CreateAsync(admin, password);
                
                if (result.Succeeded)
                {
                    // Add admin role
                    var roleAssignResult = await _userManager.AddToRoleAsync(admin, "Admin");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        await _userManager.DeleteAsync(admin);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to assign Admin role: {roleErrors}");
                    }
                    
                    return admin;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while creating admin: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (adminId == Guid.Empty)
                {
                    return false; // Invalid ID
                }
                
                // Find the admin user by ID
                var admin = await _userManager.FindByIdAsync(adminId.ToString());
                
                if (admin == null)
                {
                    return false; // Admin not found
                }
                
                // Check if user is actually in Admin role
                var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");
                if (!isAdmin)
                {
                    throw new InvalidOperationException("User is not an admin");
                }
                
                // Check if this is the last admin (prevent system lockout)
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count <= 1)
                {
                    throw new InvalidOperationException("Cannot delete the last admin in the system");
                }
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                // Delete the admin user
                var result = await _userManager.DeleteAsync(admin);
                
                if (result.Succeeded)
                {
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete admin: {errors}");
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting admin with ID {adminId}: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (adminId == Guid.Empty)
                {
                    return null; // Invalid ID
                }
                
                // Find the admin user by ID
                var admin = await _userManager.FindByIdAsync(adminId.ToString());
                
                if (admin == null)
                {
                    return null; // Admin not found
                }
                
                // Check if Admin role exists before checking user role
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    return null; // Admin role doesn't exist, so no admins possible
                }
                
                // Check if user is actually in Admin role
                var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");
                if (!isAdmin)
                {
                    return null; // User exists but is not an admin
                }
                
                return admin;
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve admin with ID {adminId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    // Return empty list if Admin role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                // Get all users in Admin role
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                
                // Convert to List and return (handles null case)
                return admins?.ToList() ?? new List<ApplicationUser>();
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve admins: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateAdminAsync(ApplicationUser admin, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (admin == null)
                    throw new ArgumentNullException(nameof(admin));
                
                if (admin.Id == Guid.Empty)
                    throw new ArgumentException("Admin ID cannot be empty", nameof(admin));

                // Check if admin exists and is actually an admin
                var existingAdmin = await GetAdminByIdAsync(admin.Id, cancellationToken);
                if (existingAdmin == null)
                {
                    throw new InvalidOperationException($"Admin with ID {admin.Id} not found or is not an admin");
                }

                // Update the admin user using UserManager
                var result = await _userManager.UpdateAsync(admin);
                
                if (result.Succeeded)
                {
                    // Ensure the admin still has the Admin role (in case it was somehow removed)
                    var isInAdminRole = await _userManager.IsInRoleAsync(admin, "Admin");
                    if (!isInAdminRole)
                    {
                        await _userManager.AddToRoleAsync(admin, "Admin");
                    }
                    
                    return admin;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update admin: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while updating admin with ID {admin?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeAdminPasswordAsync(Guid adminId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (adminId == Guid.Empty)
                    throw new ArgumentException("Admin ID cannot be empty", nameof(adminId));
                if (string.IsNullOrWhiteSpace(currentPassword))
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get admin and verify role
                var admin = await GetAdminByIdAsync(adminId, cancellationToken);
                if (admin == null)
                {
                    throw new InvalidOperationException($"Admin with ID {adminId} not found or is not an admin");
                }

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(admin, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to change admin password: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while changing admin password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetAdminPasswordAsync(Guid adminId, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (adminId == Guid.Empty)
                    throw new ArgumentException("Admin ID cannot be empty", nameof(adminId));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get admin and verify role
                var admin = await GetAdminByIdAsync(adminId, cancellationToken);
                if (admin == null)
                {
                    throw new InvalidOperationException($"Admin with ID {adminId} not found or is not an admin");
                }

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(admin);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(admin, newPassword);
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
                throw new InvalidOperationException($"Unexpected error while resetting admin password: {ex.Message}", ex);
            }
        }
    }
}
