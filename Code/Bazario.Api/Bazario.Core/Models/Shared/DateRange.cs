using System;

namespace Bazario.Core.Models.Shared
{
    /// <summary>
    /// Date range for analytics
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-12);
        public DateTime EndDate { get; set; } = DateTime.Now;
    }
}
