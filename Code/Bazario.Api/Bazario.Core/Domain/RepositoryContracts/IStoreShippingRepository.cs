using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Enums;

namespace Bazario.Core.Domain.RepositoryContracts
{
    /// <summary>
    /// Repository interface for managing store shipping rates
    /// Handles CRUD operations for store shipping rate configurations
    /// </summary>
    public interface IStoreShippingRepository
    {
        /// <summary>
        /// Adds a new store shipping rate
        /// </summary>
        Task<StoreShippingRate> AddStoreShippingRateAsync(StoreShippingRate storeShippingRate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing store shipping rate
        /// </summary>
        Task<StoreShippingRate> UpdateStoreShippingRateAsync(StoreShippingRate storeShippingRate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a store shipping rate by ID
        /// </summary>
        Task<bool> DeleteStoreShippingRateAsync(Guid storeShippingRateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a store shipping rate by ID
        /// </summary>
        Task<StoreShippingRate?> GetStoreShippingRateByIdAsync(Guid storeShippingRateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all shipping rates for a specific store
        /// </summary>
        Task<List<StoreShippingRate>> GetShippingRatesByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active shipping rates for a specific store
        /// </summary>
        Task<List<StoreShippingRate>> GetActiveShippingRatesByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific shipping rate for a store and zone
        /// </summary>
        Task<StoreShippingRate?> GetShippingRateByZoneAsync(Guid storeId, ShippingZone shippingZone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the best available shipping rate for a store and zone (active rates only)
        /// </summary>
        Task<StoreShippingRate?> GetBestShippingRateAsync(Guid storeId, ShippingZone shippingZone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates a shipping rate for a store and zone
        /// If rate exists, updates it; if not, creates a new one
        /// </summary>
        Task<StoreShippingRate> CreateOrUpdateShippingRateAsync(Guid storeId, ShippingZone shippingZone, decimal shippingCost, decimal freeShippingThreshold, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all shipping rates for multiple stores
        /// </summary>
        Task<List<StoreShippingRate>> GetShippingRatesByStoreIdsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets shipping rates by zone across all stores
        /// </summary>
        Task<List<StoreShippingRate>> GetShippingRatesByZoneAsync(ShippingZone shippingZone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a store has shipping rates configured
        /// </summary>
        Task<bool> HasShippingRatesAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of shipping rates for a store
        /// </summary>
        Task<int> GetShippingRatesCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for store shipping rates to enable efficient filtering and pagination
        /// </summary>
        IQueryable<StoreShippingRate> GetStoreShippingRatesQueryable();

        /// <summary>
        /// Gets the count of shipping rates matching the query
        /// </summary>
        Task<int> GetStoreShippingRatesCountAsync(IQueryable<StoreShippingRate> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets shipping rates with pagination from the query
        /// </summary>
        Task<List<StoreShippingRate>> GetStoreShippingRatesPagedAsync(IQueryable<StoreShippingRate> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
