using System;

namespace Bazario.Core.Exceptions
{
    /// <summary>
    /// Thrown when user account is not verified
    /// </summary>
    public class AccountNotVerifiedException : SecurityException
    {
        public AccountNotVerifiedException() : base("Account email is not verified") { }
        public AccountNotVerifiedException(string message) : base(message) { }
    }
}
