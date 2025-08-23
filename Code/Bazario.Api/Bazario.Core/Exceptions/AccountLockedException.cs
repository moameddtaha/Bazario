using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Thrown when user account is locked
    /// </summary>
    public class AccountLockedException : SecurityException
    {
        public DateTime? LockedUntil { get; }
        public string? Reason { get; }

        public AccountLockedException() : base("Account is locked due to security violations") { }
        public AccountLockedException(string reason) : base($"Account is locked: {reason}") 
        {
            Reason = reason;
        }
        public AccountLockedException(string reason, DateTime lockedUntil) : base($"Account is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss}: {reason}")
        {
            Reason = reason;
            LockedUntil = lockedUntil;
        }
    }
}
