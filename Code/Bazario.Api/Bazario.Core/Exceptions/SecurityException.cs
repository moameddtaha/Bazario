using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Base exception for security-related errors
    /// </summary>
    public abstract class SecurityException : Exception
    {
        protected SecurityException(string message) : base(message) { }
        protected SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}
