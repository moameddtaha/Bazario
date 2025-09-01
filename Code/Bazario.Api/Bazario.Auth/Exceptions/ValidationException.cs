using System;

namespace Bazario.Auth.Exceptions
{
    /// <summary>
    /// Exception for input validation errors
    /// </summary>
    public class ValidationException : Exception
    {
        public string? FieldName { get; }
        public object? InvalidValue { get; }

        public ValidationException(string message) : base(message) 
        {
            FieldName = null;
            InvalidValue = null;
        }

        public ValidationException(string message, string fieldName) : base(message)
        {
            FieldName = fieldName;
            InvalidValue = null;
        }

        public ValidationException(string message, string fieldName, object invalidValue) : base(message)
        {
            FieldName = fieldName;
            InvalidValue = invalidValue;
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException) 
        {
            FieldName = null;
            InvalidValue = null;
        }
    }
}
