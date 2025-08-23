using System;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Product performance within a store
    /// </summary>
    public class ProductPerformance
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
