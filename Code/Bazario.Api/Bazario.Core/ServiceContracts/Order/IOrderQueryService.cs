using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order query operations
    /// Handles order retrieval, searching, and filtering
    /// </summary>
    public interface IOrderQueryService
    {
        /// <summary>
        /// Retrieves an order by ID with full details
        /// </summary>
        /// <param name="orderId">Order ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order response or null if not found</returns>
        Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all orders for a specific customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customer orders</returns>
        Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves orders by status with pagination support
        /// </summary>
        /// <param name="status">Order status to filter by</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of orders</returns>
        Task<PagedResponse<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches orders with flexible filtering and pagination
        /// </summary>
        /// <param name="searchCriteria">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of orders</returns>
        Task<PagedResponse<OrderResponse>> SearchOrdersAsync(OrderSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    }
}
