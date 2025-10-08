namespace Bazario.Core.Enums.Order
{
    /// <summary>
    /// Shipping zone enumeration for delivery time calculations
    /// </summary>
    public enum ShippingZone
    {
        /// <summary>
        /// Local delivery within the same governorate (e.g., Cairo)
        /// </summary>
        Local = 1,

        /// <summary>
        /// National delivery outside the local governorate but within Egypt
        /// </summary>
        National = 2,

        /// <summary>
        /// Same-day delivery option
        /// </summary>
        SameDay = 3,

        /// <summary>
        /// Shipping not supported for this address
        /// </summary>
        NotSupported = 4
    }
}
