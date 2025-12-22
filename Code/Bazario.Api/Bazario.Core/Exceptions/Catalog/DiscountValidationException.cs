using System;

namespace Bazario.Core.Exceptions.Catalog
{
    /// <summary>
    /// Exception thrown when discount validation fails.
    /// </summary>
    public class DiscountValidationException : Exception
    {
        public string? DiscountCode { get; }
        public string ValidationError { get; }

        public DiscountValidationException(string validationError)
            : base($"Discount validation failed: {validationError}")
        {
            ValidationError = validationError;
        }

        public DiscountValidationException(string discountCode, string validationError)
            : base($"Discount validation failed for code '{discountCode}': {validationError}")
        {
            DiscountCode = discountCode;
            ValidationError = validationError;
        }

        public DiscountValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            ValidationError = message;
        }
    }
}
