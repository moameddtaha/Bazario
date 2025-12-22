using System;

namespace Bazario.Core.Exceptions.Catalog
{
    /// <summary>
    /// Exception thrown when a discount cannot be found by ID or code.
    /// </summary>
    public class DiscountNotFoundException : Exception
    {
        public Guid? DiscountId { get; }
        public string? DiscountCode { get; }

        public DiscountNotFoundException(Guid discountId)
            : base($"Discount with ID {discountId} not found")
        {
            DiscountId = discountId;
        }

        public DiscountNotFoundException(string discountCode)
            : base($"Discount with code '{discountCode}' not found")
        {
            DiscountCode = discountCode;
        }

        public DiscountNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
