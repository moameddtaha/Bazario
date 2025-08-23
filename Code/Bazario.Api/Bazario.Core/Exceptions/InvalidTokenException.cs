using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Thrown when authentication token is invalid
    /// </summary>
    public class InvalidTokenException : SecurityException
    {
        public InvalidTokenException() : base("Invalid or expired authentication token") { }
        public InvalidTokenException(string message) : base(message) { }
    }
}
