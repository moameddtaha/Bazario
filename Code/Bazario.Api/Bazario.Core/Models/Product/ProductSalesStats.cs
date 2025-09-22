using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Product
{
    /// <summary>
    /// Aggregated sales statistics for a product
    /// </summary>
    public class ProductSalesStats
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<MonthlyProductSalesData> MonthlyData { get; set; } = new();
    }
}
