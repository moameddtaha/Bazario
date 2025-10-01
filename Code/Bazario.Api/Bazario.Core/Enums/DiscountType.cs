namespace Bazario.Core.Enums
{
    /// <summary>
    /// Types of discount that can be applied to orders
    /// </summary>
    public enum DiscountType
    {
        /// <summary>
        /// Percentage discount (e.g., 10% off)
        /// </summary>
        Percentage = 1,

        /// <summary>
        /// Fixed amount discount (e.g., 50 EGP off)
        /// </summary>
        FixedAmount = 2
    }
}
