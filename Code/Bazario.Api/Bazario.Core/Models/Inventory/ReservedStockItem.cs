using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Details of a successfully reserved stock item
    /// </summary>
    public class ReservedStockItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
