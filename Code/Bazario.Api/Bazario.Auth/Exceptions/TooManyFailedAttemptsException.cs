using System;

namespace Bazario.Auth.Exceptions
{
    /// <summary>
    /// Thrown when too many failed login attempts
    /// </summary>
    public class TooManyFailedAttemptsException : SecurityException
    {
        public int RemainingAttempts { get; }
        public DateTime? LockoutUntil { get; }

        public TooManyFailedAttemptsException(int remainingAttempts) 
            : base($"Too many failed login attempts. {remainingAttempts} attempts remaining before lockout.")
        {
            RemainingAttempts = remainingAttempts;
        }

        public TooManyFailedAttemptsException(DateTime lockoutUntil) 
            : base($"Account is temporarily locked due to too many failed attempts. Try again after {lockoutUntil:yyyy-MM-dd HH:mm:ss}")
        {
            LockoutUntil = lockoutUntil;
        }
    }
}
