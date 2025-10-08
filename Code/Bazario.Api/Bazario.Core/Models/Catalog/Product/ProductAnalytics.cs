using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Catalog.Product
{
    /// <summary>
    /// Product analytics data
    /// </summary>
    public class ProductAnalytics
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int CurrentStock { get; set; }
        public int ViewCount { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public List<MonthlySalesData> MonthlySalesData { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
