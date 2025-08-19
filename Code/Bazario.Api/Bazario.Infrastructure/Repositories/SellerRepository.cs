using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Infrastructure.Repositories
{
    public class SellerRepository : ISellerRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public SellerRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<ApplicationUser> AddSellerAsync(ApplicationUser seller, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (seller == null)
                    throw new ArgumentNullException(nameof(seller));
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));

                // Create "Seller" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    var sellerRole = new ApplicationRole { Name = "Seller" };
                    var roleResult = await _roleManager.CreateAsync(sellerRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create Seller role: {roleErrors}");
                    }
                }

                // Create the seller user using UserManager with password
                var result = await _userManager.CreateAsync(seller, password);
                
                if (result.Succeeded)
                {
                    // Add seller role
                    var roleAssignResult = await _userManager.AddToRoleAsync(seller, "Seller");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        await _userManager.DeleteAsync(seller);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to assign Seller role: {roleErrors}");
                    }
                    
                    return seller;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create seller: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while creating seller: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    return false; // Invalid ID
                }
                
                // Find the seller user by ID
                var seller = await _userManager.FindByIdAsync(sellerId.ToString());
                
                if (seller == null)
                {
                    return false; // Seller not found
                }
                
                // Check if user is actually in Seller role
                var isSeller = await _userManager.IsInRoleAsync(seller, "Seller");
                if (!isSeller)
                {
                    throw new InvalidOperationException("User is not a seller");
                }
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                // Delete the seller user
                var result = await _userManager.DeleteAsync(seller);
                
                if (result.Succeeded)
                {
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete seller: {errors}");
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while deleting seller with ID {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllSellersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Seller role exists
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    // Return empty list if Seller role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                // Get all users in Seller role
                var sellers = await _userManager.GetUsersInRoleAsync("Seller");
                
                // Convert to List and return (handles null case)
                return sellers?.ToList() ?? new List<ApplicationUser>();
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve sellers: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetFilteredSellersAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                // Check if Seller role exists
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    // Return empty list if Seller role doesn't exist
                    return new List<ApplicationUser>();
                }

                // Get all sellers and filter by predicate
                var allSellers = await _userManager.GetUsersInRoleAsync("Seller");
                var filteredSellers = allSellers.AsQueryable().Where(predicate.Compile()).ToList();

                return filteredSellers;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve filtered sellers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser?> GetSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    return null; // Invalid ID
                }
                
                // Find the seller user by ID
                var seller = await _userManager.FindByIdAsync(sellerId.ToString());
                
                if (seller == null)
                {
                    return null; // Seller not found
                }
                
                // Check if Seller role exists before checking user role
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    return null; // Seller role doesn't exist, so no sellers possible
                }
                
                // Check if user is actually in Seller role
                var isSeller = await _userManager.IsInRoleAsync(seller, "Seller");
                if (!isSeller)
                {
                    return null; // User exists but is not a seller
                }
                
                return seller;
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject ILogger if needed)
                throw new InvalidOperationException($"Failed to retrieve seller with ID {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateSellerAsync(ApplicationUser seller, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (seller == null)
                    throw new ArgumentNullException(nameof(seller));
                
                if (seller.Id == Guid.Empty)
                    throw new ArgumentException("Seller ID cannot be empty", nameof(seller));

                // Check if seller exists and is actually a seller
                var existingSeller = await GetSellerByIdAsync(seller.Id, cancellationToken);
                if (existingSeller == null)
                {
                    throw new InvalidOperationException($"Seller with ID {seller.Id} not found or is not a seller");
                }

                // Update the seller user using UserManager
                var result = await _userManager.UpdateAsync(seller);
                
                if (result.Succeeded)
                {
                    // Ensure the seller still has the Seller role (in case it was somehow removed)
                    var isInSellerRole = await _userManager.IsInRoleAsync(seller, "Seller");
                    if (!isInSellerRole)
                    {
                        await _userManager.AddToRoleAsync(seller, "Seller");
                    }
                    
                    return seller;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update seller: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while updating seller with ID {seller?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeSellerPasswordAsync(Guid sellerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (sellerId == Guid.Empty)
                    throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
                if (string.IsNullOrWhiteSpace(currentPassword))
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get seller and verify role
                var seller = await GetSellerByIdAsync(sellerId, cancellationToken);
                if (seller == null)
                {
                    throw new InvalidOperationException($"Seller with ID {sellerId} not found or is not a seller");
                }

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(seller, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to change seller password: {errors}");
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
                throw new InvalidOperationException($"Unexpected error while changing seller password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetSellerPasswordAsync(Guid sellerId, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (sellerId == Guid.Empty)
                    throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));

                // Get seller and verify role
                var seller = await GetSellerByIdAsync(sellerId, cancellationToken);
                if (seller == null)
                {
                    throw new InvalidOperationException($"Seller with ID {sellerId} not found or is not a seller");
                }

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(seller);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(seller, newPassword);
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
                throw new InvalidOperationException($"Unexpected error while resetting seller password: {ex.Message}", ex);
            }
        }
    }
}
