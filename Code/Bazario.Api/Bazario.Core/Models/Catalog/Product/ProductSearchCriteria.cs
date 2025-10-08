using System;
using Bazario.Core.Enums.Catalog;

namespace Bazario.Core.Models.Catalog.Product
{
    /// <summary>
    /// Product search and filter criteria
    /// </summary>
    public class ProductSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public Guid? StoreId { get; set; }
        public Category? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; } = true;
        public bool IncludeDeleted { get; set; } = false;
        public bool OnlyDeleted { get; set; } = false;
        public string? SortBy { get; set; } = "Name"; // Name, Price, CreatedAt, Rating
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
