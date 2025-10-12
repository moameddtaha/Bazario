using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Store validation result
    /// </summary>
    public class StoreValidationResult
    {
        /// <summary>
        /// Computed property - automatically returns false if any validation errors exist
        /// </summary>
        public bool IsValid => ValidationErrors.Count == 0;

        public List<string> ValidationErrors { get; set; } = new();
        public bool NameAvailable { get; set; }
        public bool SellerEligible { get; set; }
        public int SellerStoreCount { get; set; }
        public int MaxAllowedStores { get; set; } = 10;
    }
}
