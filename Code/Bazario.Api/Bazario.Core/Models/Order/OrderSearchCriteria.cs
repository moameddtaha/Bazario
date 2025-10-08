using System;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.Models.Order
{
    /// <summary>
    /// Order search and filter criteria
    /// </summary>
    public class OrderSearchCriteria
    {
        public Guid? CustomerId { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? SortBy { get; set; } = "Date"; // Date, TotalAmount, Status
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
