using System.Collections.Generic;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Result of stock availability validation for an order
    /// Provides detailed information about which items are out of stock
    /// </summary>
    public class StockValidationResult
    {
        /// <summary>
        /// Whether all items have sufficient stock
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of items that are out of stock or have insufficient quantity
        /// </summary>
        public List<StockValidationItem> InvalidItems { get; set; } = new List<StockValidationItem>();

        /// <summary>
        /// Overall validation message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
