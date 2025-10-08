using System;
using System.Collections.Generic;

namespace Bazario.Core.Models.UserManagement
{
    /// <summary>
    /// User activity analytics
    /// </summary>
    public class UserActivityAnalytics
    {
        public Guid UserId { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int LoginCount { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int ReviewCount { get; set; }
        public List<MonthlyUserActivity> MonthlyActivity { get; set; } = new();
        public Dictionary<string, int> ActivityByType { get; set; } = new();
    }
}
