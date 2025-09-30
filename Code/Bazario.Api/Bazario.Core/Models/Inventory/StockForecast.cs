using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock forecast data for demand prediction
    /// </summary>
    public class StockForecast
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ForecastedDemand { get; set; }
        public int RecommendedReorderQuantity { get; set; }
        public DateTime ReorderDate { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public string ForecastMethod { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
    }
}
