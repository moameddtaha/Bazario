using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Thrown when user doesn't have required permission
    /// </summary>
    public class InsufficientPermissionException : SecurityException
    {
        public string RequiredPermission { get; }
        public string UserRole { get; }

        public InsufficientPermissionException(string requiredPermission, string userRole) 
            : base($"User with role '{userRole}' does not have permission '{requiredPermission}'")
        {
            RequiredPermission = requiredPermission;
            UserRole = userRole;
        }
    }
}
