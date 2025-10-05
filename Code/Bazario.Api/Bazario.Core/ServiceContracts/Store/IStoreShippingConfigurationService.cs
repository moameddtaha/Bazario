using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Store;

namespace Bazario.Core.ServiceContracts.Store
{
    /// <summary>
    /// Service contract for store shipping configuration operations
    /// </summary>
    public interface IStoreShippingConfigurationService
    {
        Task<StoreShippingConfigurationResponse> GetConfigurationAsync(Guid storeId, CancellationToken cancellationToken = default);
        Task<StoreShippingConfigurationResponse> CreateConfigurationAsync(StoreShippingConfigurationRequest request, CancellationToken cancellationToken = default);
        Task<StoreShippingConfigurationResponse> UpdateConfigurationAsync(StoreShippingConfigurationRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Hard deletes a shipping configuration (completely removes from database) - ADMIN ONLY
        /// </summary>
        /// <param name="storeId">Store ID whose configuration to delete</param>
        /// <param name="deletedBy">Admin user ID performing the deletion</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not an admin</exception>
        Task<bool> DeleteConfigurationAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);
        Task<bool> IsSameDayDeliveryAvailableAsync(Guid storeId, string city, CancellationToken cancellationToken = default);
        Task<decimal> GetDeliveryFeeAsync(Guid storeId, string city, CancellationToken cancellationToken = default);
    }
}
