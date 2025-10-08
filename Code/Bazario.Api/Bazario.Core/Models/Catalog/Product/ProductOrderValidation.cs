using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Catalog.Product
{
    /// <summary>
    /// Product order validation result
    /// </summary>
    public class ProductOrderValidation
    {
        public bool IsValid { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid StoreId { get; set; }
        public string? StoreName { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime ValidationTimestamp { get; set; }
        public bool IsInStock { get; set; }
        public bool IsActive { get; set; }
    }
}
