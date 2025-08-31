using System;
using System.Security;

namespace Bazario.Auth.Exceptions
{
    /// <summary>
    /// Thrown when user account is suspended
    /// </summary>
    public class AccountSuspendedException : SecurityException
    {
        public string? Reason { get; }
        public DateTime? SuspendedUntil { get; }

        public AccountSuspendedException() : base("Account is suspended") { }
        public AccountSuspendedException(string reason) : base($"Account is suspended: {reason}")
        {
            Reason = reason;
        }
        public AccountSuspendedException(string reason, DateTime suspendedUntil) 
            : base($"Account is suspended until {suspendedUntil:yyyy-MM-dd HH:mm:ss}: {reason}")
        {
            Reason = reason;
            SuspendedUntil = suspendedUntil;
        }
    }
}
