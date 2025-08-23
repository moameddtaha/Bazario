using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Product
{
    /// <summary>
    /// Product order validation result
    /// </summary>
    public class ProductOrderValidation
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public int AvailableQuantity { get; set; }
        public bool IsInStock { get; set; }
        public bool IsActive { get; set; }
    }
}
