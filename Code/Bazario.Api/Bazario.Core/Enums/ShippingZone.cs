namespace Bazario.Core.Enums
{
    /// <summary>
    /// Shipping zone enumeration for delivery time calculations
    /// </summary>
    public enum ShippingZone
    {
        /// <summary>
        /// Local delivery within the same city
        /// </summary>
        Local = 1,

        /// <summary>
        /// Regional delivery within the same state/province
        /// </summary>
        Regional = 2,

        /// <summary>
        /// National delivery within the same country
        /// </summary>
        National = 3,

        /// <summary>
        /// International delivery to other countries
        /// </summary>
        International = 4,

        /// <summary>
        /// Remote or hard-to-reach areas
        /// </summary>
        Remote = 5,

        /// <summary>
        /// Express delivery zones
        /// </summary>
        Express = 6,

        /// <summary>
        /// Same-day delivery zones
        /// </summary>
        SameDay = 7
    }
}
