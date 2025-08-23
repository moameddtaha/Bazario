using System;

namespace Bazario.Core.Models.Store
{
    /// <summary>
    /// Store search and filter criteria
    /// </summary>
    public class StoreSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public Guid? SellerId { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? SortBy { get; set; } = "Name"; // Name, CreatedAt, Rating, Revenue
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
