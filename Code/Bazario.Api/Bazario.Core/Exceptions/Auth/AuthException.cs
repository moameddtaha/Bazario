using System;

namespace Bazario.Core.Exceptions.Auth
{
    /// <summary>
    /// Generic authentication exception for all auth-related errors
    /// </summary>
    public class AuthException : Exception
    {
        public string ErrorCode { get; }
        public object? AdditionalData { get; }

        public AuthException(string message) : base(message)
        {
            ErrorCode = "AUTH_ERROR";
            AdditionalData = null;
        }

        public AuthException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
            AdditionalData = null;
        }

        public AuthException(string message, string errorCode, object additionalData) : base(message)
        {
            ErrorCode = errorCode;
            AdditionalData = additionalData;
        }

        public AuthException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = "AUTH_ERROR";
            AdditionalData = null;
        }

        // Common error codes
        public static class ErrorCodes
        {
            public const string InvalidCredentials = "INVALID_CREDENTIALS";
            public const string AccountLocked = "ACCOUNT_LOCKED";
            public const string AccountSuspended = "ACCOUNT_SUSPENDED";
            public const string AccountNotVerified = "ACCOUNT_NOT_VERIFIED";
            public const string TooManyAttempts = "TOO_MANY_ATTEMPTS";
            public const string InvalidToken = "INVALID_TOKEN";
            public const string InsufficientPermission = "INSUFFICIENT_PERMISSION";
            public const string UserNotFound = "USER_NOT_FOUND";
            public const string ValidationError = "VALIDATION_ERROR";
        }
    }
}
