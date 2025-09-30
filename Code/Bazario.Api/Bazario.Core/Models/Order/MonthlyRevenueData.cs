using System;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Monthly revenue data for analytics
    /// </summary>
    public class MonthlyRevenueData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }
}
