using System;

namespace Bazario.Core.Models.Product
{
    /// <summary>
    /// Product search and filter criteria
    /// </summary>
    public class ProductSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public Guid? StoreId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; } = true;
        public string? SortBy { get; set; } = "Name"; // Name, Price, CreatedAt, Rating
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
