using System;
using System.Text.RegularExpressions;

namespace Bazario.Core.Helpers.Catalog
{
    /// <summary>
    /// Helper class for product validation and logging utilities
    /// </summary>
    public class ProductValidationHelper : IProductValidationHelper
    {
        /// <summary>
        /// Checks if exception is a DbUpdateConcurrencyException without requiring direct EF Core reference
        /// </summary>
        public bool IsConcurrencyException(Exception ex)
        {
            return ex.GetType().Name == "DbUpdateConcurrencyException" ||
                   ex.GetType().FullName?.Contains("DbUpdateConcurrencyException") == true;
        }

        /// <summary>
        /// Checks if exception is a DbUpdateException without requiring direct EF Core reference
        /// </summary>
        public bool IsDatabaseUpdateException(Exception ex)
        {
            return ex.GetType().Name == "DbUpdateException" ||
                   ex.GetType().FullName?.Contains("DbUpdateException") == true;
        }

        /// <summary>
        /// Sanitizes strings for safe logging to prevent log injection
        /// </summary>
        public string SanitizeForLogging(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Remove newlines and carriage returns to prevent log injection
            var sanitized = Regex.Replace(input, @"[\r\n]", " ");
            
            // Limit length to prevent log flooding
            const int maxLength = 200;
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength) + "...";
            }

            return sanitized;
        }

        /// <summary>
        /// Gets safe display value for nullable product name in logs
        /// </summary>
        public string GetProductNameForLogging(string? productName)
        {
            return string.IsNullOrWhiteSpace(productName) ? "[Unnamed Product]" : SanitizeForLogging(productName);
        }
    }
}
