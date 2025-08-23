using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using System.Linq.Expressions;
using Bazario.Infrastructure.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AdminRepository> _logger;

        public AdminRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger<AdminRepository> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationUser> AddAdminAsync(ApplicationUser admin, string password, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new admin user: {AdminId}", admin?.Id);
            
            try
            {
                // Validate inputs
                if (admin == null)
                {
                    _logger.LogWarning("Attempted to add null admin user");
                    throw new ArgumentNullException(nameof(admin));
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Attempted to add admin with null or empty password");
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                _logger.LogDebug("Creating Admin role if it doesn't exist");

                // Create "Admin" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var adminRole = new ApplicationRole { Name = "Admin" };
                    var roleResult = await _roleManager.CreateAsync(adminRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create Admin role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to create Admin role: {roleErrors}");
                    }
                    _logger.LogDebug("Admin role created successfully");
                }
                else
                {
                    _logger.LogDebug("Admin role already exists");
                }

                _logger.LogDebug("Creating admin user with UserManager. Email: {Email}, UserName: {UserName}", 
                    admin.Email, admin.UserName);

                // Create the admin user using UserManager with password
                var result = await _userManager.CreateAsync(admin, password);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Admin user created successfully, assigning Admin role");

                    // Add admin role
                    var roleAssignResult = await _userManager.AddToRoleAsync(admin, "Admin");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        _logger.LogWarning("Role assignment failed, deleting created user to maintain consistency");
                        await _userManager.DeleteAsync(admin);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to assign Admin role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to assign Admin role: {roleErrors}");
                    }
                    
                    _logger.LogInformation("Successfully added admin user. AdminId: {AdminId}, Email: {Email}", 
                        admin.Id, admin.Email);
                    return admin;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create admin user: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create admin: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding admin user: {AdminId}", admin?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while adding admin user: {AdminId}", admin?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating admin user: {AdminId}", admin?.Id);
                throw new InvalidOperationException($"Unexpected error while creating admin: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete admin user: {AdminId}", adminId);
            
            try
            {
                // Validate input
                if (adminId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete admin with empty ID");
                    return false; // Invalid ID
                }
                
                _logger.LogDebug("Finding admin user by ID: {AdminId}", adminId);
                
                // Find the admin user by ID
                var admin = await _userManager.FindByIdAsync(adminId.ToString());
                
                if (admin == null)
                {
                    _logger.LogWarning("Admin user not found for deletion. AdminId: {AdminId}", adminId);
                    return false; // Admin not found
                }
                
                _logger.LogDebug("Verifying admin role for user: {AdminId}", adminId);
                
                // Check if user is actually in Admin role
                var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");
                if (!isAdmin)
                {
                    _logger.LogWarning("User is not an admin. AdminId: {AdminId}", adminId);
                    throw new InvalidOperationException("User is not an admin");
                }
                
                _logger.LogDebug("Checking if this is the last admin in the system");
                
                // Check if this is the last admin (prevent system lockout)
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count <= 1)
                {
                    _logger.LogWarning("Attempted to delete the last admin in the system. AdminId: {AdminId}", adminId);
                    throw new InvalidOperationException("Cannot delete the last admin in the system");
                }
                
                _logger.LogDebug("System has {AdminCount} admins, proceeding with deletion. AdminId: {AdminId}", 
                    allAdmins.Count, adminId);
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                // Delete the admin user
                var result = await _userManager.DeleteAsync(admin);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted admin user. AdminId: {AdminId}, Email: {Email}", 
                        adminId, admin.Email);
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete admin user: {Errors}. AdminId: {AdminId}", errors, adminId);
                throw new InvalidOperationException($"Failed to delete admin: {errors}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while deleting admin: {AdminId}", adminId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting admin: {AdminId}", adminId);
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
                    _logger.LogWarning("Attempted to retrieve admin with empty ID");
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
                _logger.LogError(ex, "Failed to retrieve admin: {AdminId}", adminId);
                throw new InvalidOperationException($"Failed to retrieve admin with ID {adminId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve all admin users");
            
            try
            {
                _logger.LogDebug("Checking if Admin role exists");
                
                // Check if Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    _logger.LogDebug("Admin role doesn't exist, returning empty list");
                    // Return empty list if Admin role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                _logger.LogDebug("Getting all users in Admin role");
                
                // Get all users in Admin role
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                
                // Convert to List and return (handles null case)
                var adminList = admins?.ToList() ?? new List<ApplicationUser>();
                
                _logger.LogDebug("Successfully retrieved {AdminCount} admin users", adminList.Count);
                
                return adminList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all admin users");
                throw new InvalidOperationException($"Failed to retrieve admins: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetFilteredAdminsAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered admin users");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve admins with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Checking if Admin role exists");

                // Check if Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    _logger.LogDebug("Admin role doesn't exist, returning empty list");
                    // Return empty list if Admin role doesn't exist
                    return new List<ApplicationUser>();
                }

                _logger.LogDebug("Getting all admins and applying filter");

                // Get all admins and filter by predicate
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                var filteredAdmins = allAdmins.AsQueryable().Where(predicate.Compile()).ToList();

                _logger.LogDebug("Successfully retrieved {FilteredAdminCount} filtered admin users from {TotalAdminCount} total", 
                    filteredAdmins.Count, allAdmins.Count);

                return filteredAdmins;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered admin users");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered admin users");
                throw new InvalidOperationException($"Failed to retrieve filtered admins: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateAdminAsync(ApplicationUser admin, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update admin user: {AdminId}", admin?.Id);
            
            try
            {
                // Validate input
                if (admin == null)
                {
                    _logger.LogWarning("Attempted to update null admin user");
                    throw new ArgumentNullException(nameof(admin));
                }
                
                if (admin.Id == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update admin with empty ID");
                    throw new ArgumentException("Admin ID cannot be empty", nameof(admin));
                }

                _logger.LogDebug("Verifying admin exists and has admin role. AdminId: {AdminId}", admin.Id);

                // Check if admin exists and is actually an admin
                var existingAdmin = await GetAdminByIdAsync(admin.Id, cancellationToken);
                if (existingAdmin == null)
                {
                    _logger.LogWarning("Admin not found or is not an admin. AdminId: {AdminId}", admin.Id);
                    throw new InvalidOperationException($"Admin with ID {admin.Id} not found or is not an admin");
                }

                _logger.LogDebug("Updating admin user with UserManager. AdminId: {AdminId}", admin.Id);

                // Update the admin user using UserManager
                var result = await _userManager.UpdateAsync(admin);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Admin user updated successfully, ensuring Admin role is maintained");

                    // Ensure the admin still has the Admin role (in case it was somehow removed)
                    var isInAdminRole = await _userManager.IsInRoleAsync(admin, "Admin");
                    if (!isInAdminRole)
                    {
                        _logger.LogDebug("Admin role was missing, reassigning it. AdminId: {AdminId}", admin.Id);
                        await _userManager.AddToRoleAsync(admin, "Admin");
                    }
                    
                    _logger.LogInformation("Successfully updated admin user. AdminId: {AdminId}, Email: {Email}", 
                        admin.Id, admin.Email);
                    return admin;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update admin user: {Errors}. AdminId: {AdminId}", errors, admin.Id);
                throw new InvalidOperationException($"Failed to update admin: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating admin user: {AdminId}", admin?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating admin user: {AdminId}", admin?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating admin user: {AdminId}", admin?.Id);
                throw new InvalidOperationException($"Unexpected error while updating admin with ID {admin?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeAdminPasswordAsync(Guid adminId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to change admin password: {AdminId}", adminId);
            
            try
            {
                // Validate inputs
                if (adminId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to change password with empty admin ID");
                    throw new ArgumentException("Admin ID cannot be empty", nameof(adminId));
                }
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty current password. AdminId: {AdminId}", adminId);
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty new password. AdminId: {AdminId}", adminId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting admin and verifying role. AdminId: {AdminId}", adminId);

                // Get admin and verify role
                var admin = await GetAdminByIdAsync(adminId, cancellationToken);
                if (admin == null)
                {
                    _logger.LogWarning("Admin not found or is not an admin. AdminId: {AdminId}", adminId);
                    throw new InvalidOperationException($"Admin with ID {adminId} not found or is not an admin");
                }

                _logger.LogDebug("Changing password using UserManager. AdminId: {AdminId}", adminId);

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(admin, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to change admin password: {Errors}. AdminId: {AdminId}", errors, adminId);
                    throw new InvalidOperationException($"Failed to change admin password: {errors}");
                }

                _logger.LogInformation("Successfully changed admin password. AdminId: {AdminId}", adminId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while changing admin password: {AdminId}", adminId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while changing admin password: {AdminId}", adminId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while changing admin password: {AdminId}", adminId);
                throw new InvalidOperationException($"Unexpected error while changing admin password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetAdminPasswordAsync(Guid adminId, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to reset admin password: {AdminId}", adminId);
            
            try
            {
                // Validate inputs
                if (adminId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to reset password with empty admin ID");
                    throw new ArgumentException("Admin ID cannot be empty", nameof(adminId));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to reset password with empty new password. AdminId: {AdminId}", adminId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting admin and verifying role. AdminId: {AdminId}", adminId);

                // Get admin and verify role
                var admin = await GetAdminByIdAsync(adminId, cancellationToken);
                if (admin == null)
                {
                    _logger.LogWarning("Admin not found or is not an admin. AdminId: {AdminId}", adminId);
                    throw new InvalidOperationException($"Admin with ID {adminId} not found or is not an admin");
                }

                _logger.LogDebug("Removing current password. AdminId: {AdminId}", adminId);

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(admin);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to remove current password: {Errors}. AdminId: {AdminId}", errors, adminId);
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                _logger.LogDebug("Adding new password. AdminId: {AdminId}", adminId);

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(admin, newPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to set new password: {Errors}. AdminId: {AdminId}", errors, adminId);
                    throw new InvalidOperationException($"Failed to set new password: {errors}");
                }

                _logger.LogInformation("Successfully reset admin password. AdminId: {AdminId}", adminId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while resetting admin password: {AdminId}", adminId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while resetting admin password: {AdminId}", adminId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while resetting admin password: {AdminId}", adminId);
                throw new InvalidOperationException($"Unexpected error while resetting admin password: {ex.Message}", ex);
            }
        }
    }
}
