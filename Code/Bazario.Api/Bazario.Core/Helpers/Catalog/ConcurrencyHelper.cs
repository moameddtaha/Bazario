using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Helpers.Catalog
{
    /// <summary>
    /// Helper class for handling optimistic concurrency conflicts with retry logic
    /// </summary>
    public class ConcurrencyHelper : IConcurrencyHelper
    {
        private readonly ILogger<ConcurrencyHelper> _logger;
        
        // Retry configuration
        private const int MaxRetries = 3;
        private const int BaseDelayMs = 50;

        public ConcurrencyHelper(ILogger<ConcurrencyHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an operation with automatic retry logic for concurrency exceptions
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation, 
            string operationName,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            
            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsConcurrencyException(ex))
                {
                    retryCount++;
                    
                    if (retryCount > MaxRetries)
                    {
                        _logger.LogError(ex, 
                            "Max retries ({MaxRetries}) reached for concurrency conflict in operation: {OperationName}", 
                            MaxRetries, operationName);
                        throw;
                    }

                    _logger.LogWarning(
                        "Concurrency conflict detected in operation: {OperationName}. Retrying ({RetryCount}/{MaxRetries})...", 
                        operationName, retryCount, MaxRetries);

                    // Add exponential backoff with random jitter to prevent thundering herd
                    var delay = BaseDelayMs * retryCount + new Random().Next(0, 20);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Checks if exception is a DbUpdateConcurrencyException without requiring direct EF Core reference
        /// </summary>
        private static bool IsConcurrencyException(Exception ex)
        {
            return ex.GetType().Name == "DbUpdateConcurrencyException" ||
                   ex.GetType().FullName?.Contains("DbUpdateConcurrencyException") == true;
        }
    }
}
