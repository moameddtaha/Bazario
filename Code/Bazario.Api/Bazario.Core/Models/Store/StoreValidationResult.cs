using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Store validation result
    /// </summary>
    public class StoreValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool NameAvailable { get; set; }
        public bool SellerEligible { get; set; }
        public int SellerStoreCount { get; set; }
        public int MaxAllowedStores { get; set; } = 10;
    }
}
