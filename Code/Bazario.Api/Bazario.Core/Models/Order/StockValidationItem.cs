using System;
using Bazario.Core.Enums.Inventory;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Detailed information about a specific item's stock validation failure
    /// </summary>
    public class StockValidationItem
    {
        /// <summary>
        /// Product ID that failed validation
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name (if product exists)
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Quantity requested in the order
        /// </summary>
        public int RequestedQuantity { get; set; }

        /// <summary>
        /// Available quantity in stock (null if product doesn't exist)
        /// </summary>
        public int? AvailableQuantity { get; set; }

        /// <summary>
        /// Reason for validation failure
        /// </summary>
        public StockValidationFailureReason FailureReason { get; set; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
