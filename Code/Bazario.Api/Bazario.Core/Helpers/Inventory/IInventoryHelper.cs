using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;

namespace Bazario.Core.Helpers.Inventory
{
    /// <summary>
    /// Helper interface for inventory-related operations
    /// </summary>
    public interface IInventoryHelper
    {
        /// <summary>
        /// Gets the store ID for a given product
        /// </summary>
        Task<Guid> GetStoreIdForProductAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total sales quantity for a product within a date range
        /// </summary>
        Task<int> GetProductSalesQuantityAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total revenue for a product within a date range
        /// </summary>
        Task<decimal> GetProductRevenueAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets alert preferences for a store
        /// </summary>
        InventoryAlertPreferences GetAlertPreferences(Guid storeId, Dictionary<Guid, InventoryAlertPreferences> alertPreferences);

        /// <summary>
        /// Creates HTML body for bulk low stock alert email
        /// </summary>
        string CreateBulkAlertEmailBody(List<LowStockAlert> alerts, Guid storeId);
    }
}
