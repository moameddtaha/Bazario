using System;

namespace Bazario.Core.Options
{
    /// <summary>
    /// Configuration options for product validation service
    /// </summary>
    public class ProductValidationOptions
    {
        /// <summary>
        /// Maximum allowed product price (default: 1,000,000)
        /// </summary>
        public decimal MaximumProductPrice { get; set; } = 1_000_000;

        /// <summary>
        /// Number of days to look back for pending reservations (default: 7)
        /// </summary>
        public int ReservationLookbackDays { get; set; } = 7;

        /// <summary>
        /// Maximum allowed order total (default: decimal.MaxValue / 2)
        /// </summary>
        public decimal MaximumOrderTotal { get; set; } = decimal.MaxValue / 2;
    }
}
