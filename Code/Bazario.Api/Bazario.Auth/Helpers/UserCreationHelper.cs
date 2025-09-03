using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums;
using Bazario.Auth.DTO;

namespace Bazario.Auth.Helpers
{
    /// <summary>
    /// Static helper class for user creation and validation operations
    /// </summary>
    public static class UserCreationHelper
    {
        /// <summary>
        /// Creates a new ApplicationUser from registration request
        /// </summary>
        public static ApplicationUser CreateUserFromRequest(RegisterRequest request)
        {
            return new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender?.ToString(),
                Age = request.Age,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Validates if a role is valid for registration
        /// </summary>
        public static bool IsValidRole(Role role)
        {
            return role == Role.Customer || role == Role.Seller;
        }

        /// <summary>
        /// Gets a valid user name for display purposes
        /// </summary>
        public static string GetValidUserName(ApplicationUser user)
        {
            // Try to get a meaningful name, fallback to email, then to generic "User"
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                return user.FirstName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }
            
            return "User";
        }

        /// <summary>
        /// Gets a user name for email purposes (full name preferred)
        /// </summary>
        public static string GetUserNameForEmail(ApplicationUser user)
        {
            // Try to get a meaningful name, fallback to email, then to generic "User"
            if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                return user.FirstName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }
            
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }
            
            return "User";
        }
    }
}
