using System;

namespace Bazario.Core.Helpers.Catalog
{
    /// <summary>
    /// Helper interface for product validation and logging utilities
    /// </summary>
    public interface IProductValidationHelper
    {
        /// <summary>
        /// Checks if exception is a concurrency exception
        /// </summary>
        bool IsConcurrencyException(Exception ex);

        /// <summary>
        /// Checks if exception is a database update exception
        /// </summary>
        bool IsDatabaseUpdateException(Exception ex);

        /// <summary>
        /// Sanitizes strings for safe logging to prevent log injection
        /// </summary>
        string SanitizeForLogging(string? input);

        /// <summary>
        /// Gets safe display value for nullable product name in logs
        /// </summary>
        string GetProductNameForLogging(string? productName);
    }
}
