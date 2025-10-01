using Bazario.Core.Domain.Entities;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Represents an order with pre-calculated discount code count for performance optimization
    /// </summary>
    public class OrderWithCodeCount
    {
        public Domain.Entities.Order Order { get; set; } = null!;
        public int CodeCount { get; set; }
        public decimal ProportionalDiscountAmount { get; set; }
        public decimal ProportionalTotalAmount { get; set; }
    }
}
