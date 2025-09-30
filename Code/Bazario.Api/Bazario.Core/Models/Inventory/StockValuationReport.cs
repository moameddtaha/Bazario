using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock valuation report for a store
    /// </summary>
    public class StockValuationReport
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalStockValue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalQuantity { get; set; }
        public decimal AverageProductValue { get; set; }
        public List<ProductValuation> ProductValuations { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
