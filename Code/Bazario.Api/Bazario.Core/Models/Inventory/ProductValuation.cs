using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Individual product valuation
    /// </summary>
    public class ProductValuation
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }
}
