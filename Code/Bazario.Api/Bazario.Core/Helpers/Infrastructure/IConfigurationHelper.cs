namespace Bazario.Core.Helpers.Infrastructure
{
    /// <summary>
    /// Helper for safely retrieving and parsing configuration values with fallback defaults
    /// </summary>
    public interface IConfigurationHelper
    {
        /// <summary>
        /// Gets a configuration value with type conversion and culture-invariant parsing.
        /// Returns the default value if the key is not found, value is empty, or parsing fails.
        /// </summary>
        /// <typeparam name="T">The target type for the configuration value</typeparam>
        /// <param name="key">The configuration key to retrieve (e.g., "Validation:MaximumPageSize")</param>
        /// <param name="defaultValue">The fallback value if retrieval or parsing fails</param>
        /// <returns>The parsed configuration value or the default value</returns>
        T GetValue<T>(string key, T defaultValue);
    }
}
