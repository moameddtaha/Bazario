namespace Bazario.Core.Enums.Inventory
{
    /// <summary>
    /// Reasons why stock validation might fail
    /// </summary>
    public enum StockValidationFailureReason
    {
        /// <summary>
        /// Product doesn't exist in the database
        /// </summary>
        ProductNotFound,

        /// <summary>
        /// Requested quantity exceeds available stock
        /// </summary>
        InsufficientStock,

        /// <summary>
        /// Product is inactive or unavailable
        /// </summary>
        ProductInactive,

        /// <summary>
        /// Product stock quantity is zero
        /// </summary>
        OutOfStock
    }
}
