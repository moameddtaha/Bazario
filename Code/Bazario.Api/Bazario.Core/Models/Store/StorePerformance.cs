using System;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Store performance summary
    /// </summary>
    public class StorePerformance
    {
        public Guid StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? Category { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int ProductCount { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int Rank { get; set; }
    }
}
