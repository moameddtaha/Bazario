using System;

namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Low stock alert
    /// </summary>
    public class LowStockAlert
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid StoreId { get; set; }
        public string? StoreName { get; set; }
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime AlertDate { get; set; }
    }
}
