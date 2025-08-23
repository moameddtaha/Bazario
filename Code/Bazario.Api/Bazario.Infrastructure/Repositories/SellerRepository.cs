using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Domain.RepositoryContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    public class SellerRepository : ISellerRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<SellerRepository> _logger;

        public SellerRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger<SellerRepository> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationUser> AddSellerAsync(ApplicationUser seller, string password, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to add new seller user: {SellerId}", seller?.Id);
            
            try
            {
                // Validate inputs
                if (seller == null)
                {
                    _logger.LogWarning("Attempted to add null seller user");
                    throw new ArgumentNullException(nameof(seller));
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Attempted to add seller with null or empty password");
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                _logger.LogDebug("Creating Seller role if it doesn't exist");

                // Create "Seller" role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    var sellerRole = new ApplicationRole { Name = "Seller" };
                    var roleResult = await _roleManager.CreateAsync(sellerRole);
                    
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create Seller role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to create Seller role: {roleErrors}");
                    }
                    _logger.LogDebug("Seller role created successfully");
                }
                else
                {
                    _logger.LogDebug("Seller role already exists");
                }

                _logger.LogDebug("Creating seller user with UserManager. Email: {Email}, UserName: {UserName}", 
                    seller.Email, seller.UserName);

                // Create the seller user using UserManager with password
                var result = await _userManager.CreateAsync(seller, password);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Seller user created successfully, assigning Seller role");

                    // Add seller role
                    var roleAssignResult = await _userManager.AddToRoleAsync(seller, "Seller");
                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, delete the created user to maintain consistency
                        _logger.LogWarning("Role assignment failed, deleting created user to maintain consistency");
                        await _userManager.DeleteAsync(seller);
                        var roleErrors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to assign Seller role: {RoleErrors}", roleErrors);
                        throw new InvalidOperationException($"Failed to assign Seller role: {roleErrors}");
                    }
                    
                    _logger.LogInformation("Successfully added seller user. SellerId: {SellerId}, Email: {Email}", 
                        seller.Id, seller.Email);
                    return seller;
                }
                
                // If creation failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create seller user: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create seller: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding seller user: {SellerId}", seller?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while adding seller user: {SellerId}", seller?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating seller user: {SellerId}", seller?.Id);
                throw new InvalidOperationException($"Unexpected error while creating seller: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to delete seller user: {SellerId}", sellerId);
            
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete seller with empty ID");
                    return false; // Invalid ID
                }
                
                _logger.LogDebug("Finding seller user by ID: {SellerId}", sellerId);
                
                // Find the seller user by ID
                var seller = await _userManager.FindByIdAsync(sellerId.ToString());
                
                if (seller == null)
                {
                    _logger.LogWarning("Seller user not found for deletion. SellerId: {SellerId}", sellerId);
                    return false; // Seller not found
                }
                
                _logger.LogDebug("Verifying seller role for user: {SellerId}", sellerId);
                
                // Check if user is actually in Seller role
                var isSeller = await _userManager.IsInRoleAsync(seller, "Seller");
                if (!isSeller)
                {
                    _logger.LogWarning("User is not a seller. SellerId: {SellerId}", sellerId);
                    throw new InvalidOperationException("User is not a seller");
                }
                
                // Remember: You need to delete Reviews + OrderItems first before deleting user
                // This is a reminder for when you implement delete logic in services
                
                _logger.LogDebug("Proceeding with seller deletion. SellerId: {SellerId}", sellerId);
                
                // Delete the seller user
                var result = await _userManager.DeleteAsync(seller);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted seller user. SellerId: {SellerId}, Email: {Email}", 
                        sellerId, seller.Email);
                    return true;
                }
                
                // If deletion failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete seller user: {Errors}. SellerId: {SellerId}", errors, sellerId);
                throw new InvalidOperationException($"Failed to delete seller: {errors}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while deleting seller: {SellerId}", sellerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting seller: {SellerId}", sellerId);
                throw new InvalidOperationException($"Unexpected error while deleting seller with ID {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetAllSellersAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve all seller users");
            
            try
            {
                _logger.LogDebug("Checking if Seller role exists");
                
                // Check if Seller role exists
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    _logger.LogDebug("Seller role doesn't exist, returning empty list");
                    // Return empty list if Seller role doesn't exist
                    return new List<ApplicationUser>();
                }
                
                _logger.LogDebug("Getting all users in Seller role");
                
                // Get all users in Seller role
                var sellers = await _userManager.GetUsersInRoleAsync("Seller");
                
                // Convert to List and return (handles null case)
                var sellerList = sellers?.ToList() ?? new List<ApplicationUser>();
                
                _logger.LogDebug("Successfully retrieved {SellerCount} seller users", sellerList.Count);
                
                return sellerList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all seller users");
                throw new InvalidOperationException($"Failed to retrieve sellers: {ex.Message}", ex);
            }
        }

        public async Task<List<ApplicationUser>> GetFilteredSellersAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve filtered seller users");
            
            try
            {
                // Validate input
                if (predicate == null)
                {
                    _logger.LogWarning("Attempted to retrieve sellers with null predicate");
                    throw new ArgumentNullException(nameof(predicate));
                }

                _logger.LogDebug("Checking if Seller role exists");

                // Check if Seller role exists
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    _logger.LogDebug("Seller role doesn't exist, returning empty list");
                    // Return empty list if Seller role doesn't exist
                    return new List<ApplicationUser>();
                }

                _logger.LogDebug("Getting all sellers and applying filter");

                // Get all sellers and filter by predicate
                var allSellers = await _userManager.GetUsersInRoleAsync("Seller");
                var filteredSellers = allSellers.AsQueryable().Where(predicate.Compile()).ToList();

                _logger.LogDebug("Successfully retrieved {FilteredSellerCount} filtered seller users from {TotalSellerCount} total", 
                    filteredSellers.Count, allSellers.Count);

                return filteredSellers;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while retrieving filtered seller users");
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve filtered seller users");
                throw new InvalidOperationException($"Failed to retrieve filtered sellers: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser?> GetSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting to retrieve seller by ID: {SellerId}", sellerId);
            
            try
            {
                // Validate input
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to retrieve seller with empty ID");
                    return null; // Invalid ID
                }
                
                _logger.LogDebug("Finding seller user by ID: {SellerId}", sellerId);
                
                // Find the seller user by ID
                var seller = await _userManager.FindByIdAsync(sellerId.ToString());
                
                if (seller == null)
                {
                    _logger.LogDebug("Seller user not found. SellerId: {SellerId}", sellerId);
                    return null; // Seller not found
                }
                
                _logger.LogDebug("Checking if Seller role exists");
                
                // Check if Seller role exists before checking user role
                if (!await _roleManager.RoleExistsAsync("Seller"))
                {
                    _logger.LogDebug("Seller role doesn't exist, no sellers possible. SellerId: {SellerId}", sellerId);
                    return null; // Seller role doesn't exist, so no sellers possible
                }
                
                _logger.LogDebug("Verifying seller role for user: {SellerId}", sellerId);
                
                // Check if user is actually in Seller role
                var isSeller = await _userManager.IsInRoleAsync(seller, "Seller");
                if (!isSeller)
                {
                    _logger.LogDebug("User exists but is not a seller. SellerId: {SellerId}", sellerId);
                    return null; // User exists but is not a seller
                }
                
                _logger.LogDebug("Successfully retrieved seller user. SellerId: {SellerId}, Email: {Email}", 
                    sellerId, seller.Email);
                
                return seller;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve seller: {SellerId}", sellerId);
                throw new InvalidOperationException($"Failed to retrieve seller with ID {sellerId}: {ex.Message}", ex);
            }
        }

        public async Task<ApplicationUser> UpdateSellerAsync(ApplicationUser seller, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to update seller user: {SellerId}", seller?.Id);
            
            try
            {
                // Validate input
                if (seller == null)
                {
                    _logger.LogWarning("Attempted to update null seller user");
                    throw new ArgumentNullException(nameof(seller));
                }
                
                if (seller.Id == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to update seller with empty ID");
                    throw new ArgumentException("Seller ID cannot be empty", nameof(seller));
                }

                _logger.LogDebug("Verifying seller exists and has seller role. SellerId: {SellerId}", seller.Id);

                // Check if seller exists and is actually a seller
                var existingSeller = await GetSellerByIdAsync(seller.Id, cancellationToken);
                if (existingSeller == null)
                {
                    _logger.LogWarning("Seller not found or is not a seller. SellerId: {SellerId}", seller.Id);
                    throw new InvalidOperationException($"Seller with ID {seller.Id} not found or is not a seller");
                }

                _logger.LogDebug("Updating seller user with UserManager. SellerId: {SellerId}", seller.Id);

                // Update the seller user using UserManager
                var result = await _userManager.UpdateAsync(seller);
                
                if (result.Succeeded)
                {
                    _logger.LogDebug("Seller user updated successfully, ensuring Seller role is maintained");

                    // Ensure the seller still has the Seller role (in case it was somehow removed)
                    var isInSellerRole = await _userManager.IsInRoleAsync(seller, "Seller");
                    if (!isInSellerRole)
                    {
                        _logger.LogDebug("Seller role was missing, reassigning it. SellerId: {SellerId}", seller.Id);
                        await _userManager.AddToRoleAsync(seller, "Seller");
                    }
                    
                    _logger.LogInformation("Successfully updated seller user. SellerId: {SellerId}, Email: {Email}", 
                        seller.Id, seller.Email);
                    return seller;
                }
                
                // If update failed, throw exception with details
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update seller user: {Errors}. SellerId: {SellerId}", errors, seller.Id);
                throw new InvalidOperationException($"Failed to update seller: {errors}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating seller user: {SellerId}", seller?.Id);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while updating seller user: {SellerId}", seller?.Id);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating seller user: {SellerId}", seller?.Id);
                throw new InvalidOperationException($"Unexpected error while updating seller with ID {seller?.Id}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ChangeSellerPasswordAsync(Guid sellerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to change seller password: {SellerId}", sellerId);
            
            try
            {
                // Validate inputs
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to change password with empty seller ID");
                    throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
                }
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty current password. SellerId: {SellerId}", sellerId);
                    throw new ArgumentException("Current password cannot be empty", nameof(currentPassword));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to change password with empty new password. SellerId: {SellerId}", sellerId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting seller and verifying role. SellerId: {SellerId}", sellerId);

                // Get seller and verify role
                var seller = await GetSellerByIdAsync(sellerId, cancellationToken);
                if (seller == null)
                {
                    _logger.LogWarning("Seller not found or is not a seller. SellerId: {SellerId}", sellerId);
                    throw new InvalidOperationException($"Seller with ID {sellerId} not found or is not a seller");
                }

                _logger.LogDebug("Changing password using UserManager. SellerId: {SellerId}", sellerId);

                // Change password using UserManager (this validates current password)
                var result = await _userManager.ChangePasswordAsync(seller, currentPassword, newPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to change seller password: {Errors}. SellerId: {SellerId}", errors, sellerId);
                    throw new InvalidOperationException($"Failed to change seller password: {errors}");
                }

                _logger.LogInformation("Successfully changed seller password. SellerId: {SellerId}", sellerId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while changing seller password: {SellerId}", sellerId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while changing seller password: {SellerId}", sellerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while changing seller password: {SellerId}", sellerId);
                throw new InvalidOperationException($"Unexpected error while changing seller password: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetSellerPasswordAsync(Guid sellerId, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to reset seller password: {SellerId}", sellerId);
            
            try
            {
                // Validate inputs
                if (sellerId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to reset password with empty seller ID");
                    throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
                }
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    _logger.LogWarning("Attempted to reset password with empty new password. SellerId: {SellerId}", sellerId);
                    throw new ArgumentException("New password cannot be empty", nameof(newPassword));
                }

                _logger.LogDebug("Getting seller and verifying role. SellerId: {SellerId}", sellerId);

                // Get seller and verify role
                var seller = await GetSellerByIdAsync(sellerId, cancellationToken);
                if (seller == null)
                {
                    _logger.LogWarning("Seller not found or is not a seller. SellerId: {SellerId}", sellerId);
                    throw new InvalidOperationException($"Seller with ID {sellerId} not found or is not a seller");
                }

                _logger.LogDebug("Removing current password. SellerId: {SellerId}", sellerId);

                // Remove current password
                var removeResult = await _userManager.RemovePasswordAsync(seller);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to remove current password: {Errors}. SellerId: {SellerId}", errors, sellerId);
                    throw new InvalidOperationException($"Failed to remove current password: {errors}");
                }

                _logger.LogDebug("Adding new password. SellerId: {SellerId}", sellerId);

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(seller, newPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to set new password: {Errors}. SellerId: {SellerId}", errors, sellerId);
                    throw new InvalidOperationException($"Failed to set new password: {errors}");
                }

                _logger.LogInformation("Successfully reset seller password. SellerId: {SellerId}", sellerId);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while resetting seller password: {SellerId}", sellerId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while resetting seller password: {SellerId}", sellerId);
                throw; // Re-throw our custom exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while resetting seller password: {SellerId}", sellerId);
                throw new InvalidOperationException($"Unexpected error while resetting seller password: {ex.Message}", ex);
            }
        }
    }
}
