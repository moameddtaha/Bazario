using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Bazario.Core.Helpers.Infrastructure
{
    /// <summary>
    /// Helper implementation for safely retrieving configuration values with type conversion and fallback defaults.
    /// Supports nullable types and uses culture-invariant parsing for consistent behavior across different locales.
    /// </summary>
    public class ConfigurationHelper : IConfigurationHelper
    {
        private readonly IConfiguration _configuration;

        public ConfigurationHelper(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets a configuration value with type conversion and culture-invariant parsing.
        /// Returns the default value if the key is not found, value is empty, or parsing fails.
        /// </summary>
        /// <typeparam name="T">The target type for the configuration value</typeparam>
        /// <param name="key">The configuration key to retrieve (e.g., "Validation:MaximumPageSize")</param>
        /// <param name="defaultValue">The fallback value if retrieval or parsing fails</param>
        /// <returns>The parsed configuration value or the default value</returns>
        public T GetValue<T>(string key, T defaultValue)
        {
            if (_configuration == null)
                return defaultValue;

            var value = _configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            try
            {
                var targetType = typeof(T);
                // Handle nullable types by getting the underlying type
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // Use InvariantCulture for consistent parsing across different locales
                return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                // Log parsing failures for debugging (using Debug.WriteLine to avoid circular dependency)
                Debug.WriteLine($"Failed to parse configuration value '{key}' = '{value}': {ex.Message}");
                return defaultValue;
            }
        }
    }
}
