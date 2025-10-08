using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums.Authentication;
using Bazario.Core.Models.UserManagement;

namespace Bazario.Core.Helpers.UserManagement
{
    /// <summary>
    /// Helper functions for creating UserResponse models
    /// </summary>
    public static class UserResponseHelper
    {
        /// <summary>
        /// Creates a UserResponse from ApplicationUser and roles
        /// </summary>
        /// <param name="user">The user to convert</param>
        /// <param name="roles">The user's roles</param>
        /// <returns>UserResponse model</returns>
        /// <exception cref="InvalidOperationException">Thrown when role parsing fails</exception>
        public static UserResponse CreateUserResponse(ApplicationUser user, List<string> roles)
        {
            // Get the primary role (first role in the list) and convert to Role enum
            var primaryRoleString = roles.FirstOrDefault();
            
            if (string.IsNullOrEmpty(primaryRoleString))
            {
                throw new InvalidOperationException($"No role found for user {user.Id} ({user.Email})");
            }

            // Parse the role string to Role enum
            if (Enum.TryParse<Role>(primaryRoleString, true, out var role))
            {
                return new UserResponse()
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    Gender = !string.IsNullOrEmpty(user.Gender) ? Enum.Parse<Gender>(user.Gender) : null,
                    Age = user.Age,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    Role = role,
                    Status = UserStatus.Active, // Add the missing property
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };
            }
            
            throw new InvalidOperationException($"Failed to parse role '{primaryRoleString}' for user {user.Id} ({user.Email}). Expected: Customer, Seller, or Admin");
        }
    }
}
