using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Thrown when user credentials are invalid
    /// </summary>
    public class InvalidCredentialsException : SecurityException
    {
        public InvalidCredentialsException() : base("Invalid email/username or password") { }
        public InvalidCredentialsException(string message) : base(message) { }
    }
}
