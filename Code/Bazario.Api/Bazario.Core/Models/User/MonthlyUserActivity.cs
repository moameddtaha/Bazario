using System;

namespace Bazario.Core.Models.User
{
    /// <summary>
    /// Monthly user activity data
    /// </summary>
    public class MonthlyUserActivity
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Logins { get; set; }
        public int Orders { get; set; }
        public decimal AmountSpent { get; set; }
        public int Reviews { get; set; }
    }
}
