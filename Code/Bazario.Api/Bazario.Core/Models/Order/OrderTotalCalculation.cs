using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Order total calculation result
    /// </summary>
    public class OrderTotalCalculation
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public List<string> AppliedDiscounts { get; set; } = new();
    }
}
