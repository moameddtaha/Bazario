using System;

namespace Bazario.Core.Models.UserManagement
{
    /// <summary>
    /// User search criteria
    /// </summary>
    public class UserSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public UserStatus? Status { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
