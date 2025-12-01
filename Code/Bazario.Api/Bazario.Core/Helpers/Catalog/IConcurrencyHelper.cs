using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazario.Core.Helpers.Catalog
{
    /// <summary>
    /// Helper interface for handling optimistic concurrency conflicts with retry logic
    /// </summary>
    public interface IConcurrencyHelper
    {
        /// <summary>
        /// Executes an operation with automatic retry logic for concurrency exceptions
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for logging purposes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation, 
            string operationName,
            CancellationToken cancellationToken = default);
    }
}
