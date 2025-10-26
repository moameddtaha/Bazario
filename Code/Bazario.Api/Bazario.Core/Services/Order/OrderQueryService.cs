using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Extensions.Order;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;
using OrderEntity = Bazario.Core.Domain.Entities.Order.Order;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order query operations
    /// Handles order retrieval, searching, and filtering
    /// </summary>
    public class OrderQueryService : IOrderQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderQueryService> _logger;
        private const int MaxPageSize = 100; // Maximum allowed page size to prevent DoS attacks

        public OrderQueryService(
            IUnitOfWork unitOfWork,
            ILogger<OrderQueryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            _logger.LogDebug("Retrieving order by ID: {OrderId}", orderId);

            try
            {
                var order = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogDebug("Order not found: {OrderId}", orderId);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved order: {OrderId}, Customer: {CustomerId}",
                    order.OrderId, order.CustomerId);

                return order.ToOrderResponse();
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to retrieve order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (customerId == Guid.Empty)
            {
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
            }

            _logger.LogDebug("Retrieving orders for customer: {CustomerId}", customerId);

            try
            {
                var orders = await _unitOfWork.Orders.GetOrdersByCustomerIdAsync(customerId, cancellationToken);

                _logger.LogDebug("Successfully retrieved {Count} orders for customer: {CustomerId}",
                    orders.Count, customerId);

                return orders.Select(o => o.ToOrderResponse()).ToList();
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for customer: {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to retrieve orders for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<PagedResponse<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (pageNumber < 1)
            {
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
            }

            if (pageSize > MaxPageSize)
            {
                throw new ArgumentException($"Page size cannot exceed {MaxPageSize}", nameof(pageSize));
            }

            _logger.LogDebug("Retrieving orders by status: {Status}, Page: {PageNumber}, Size: {PageSize}",
                status, pageNumber, pageSize);

            try
            {
                // Note: Current repository implementation loads all orders with the status, then paginates in memory
                // This is inefficient for large datasets. TODO: Add database-level pagination to repository
                var allOrders = await _unitOfWork.Orders.GetOrdersByStatusAsync(status, cancellationToken);
                var totalCount = allOrders.Count;

                var pagedOrders = allOrders
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var orderResponses = pagedOrders.Select(o => o.ToOrderResponse()).ToList();

                var result = new PagedResponse<OrderResponse>
                {
                    Items = orderResponses,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogDebug("Successfully retrieved orders by status: {Status}. Found {TotalCount} orders, returning page {PageNumber} with {ItemCount} items",
                    status, totalCount, pageNumber, orderResponses.Count);

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders by status: {Status}", status);
                throw new InvalidOperationException($"Failed to retrieve orders by status {status}: {ex.Message}", ex);
            }
        }

        public async Task<PagedResponse<OrderResponse>> SearchOrdersAsync(OrderSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (searchCriteria == null)
            {
                throw new ArgumentNullException(nameof(searchCriteria), "Search criteria cannot be null");
            }

            if (searchCriteria.PageNumber < 1)
            {
                throw new ArgumentException("Page number must be greater than 0", nameof(searchCriteria));
            }

            if (searchCriteria.PageSize < 1)
            {
                throw new ArgumentException("Page size must be greater than 0", nameof(searchCriteria));
            }

            if (searchCriteria.PageSize > MaxPageSize)
            {
                throw new ArgumentException($"Page size cannot exceed {MaxPageSize}", nameof(searchCriteria));
            }

            if (searchCriteria.StartDate.HasValue && searchCriteria.EndDate.HasValue && searchCriteria.StartDate > searchCriteria.EndDate)
            {
                throw new ArgumentException("Start date cannot be greater than end date", nameof(searchCriteria));
            }

            if (searchCriteria.MinAmount.HasValue && searchCriteria.MaxAmount.HasValue && searchCriteria.MinAmount > searchCriteria.MaxAmount)
            {
                throw new ArgumentException("Minimum amount cannot be greater than maximum amount", nameof(searchCriteria));
            }

            _logger.LogDebug("Searching orders with criteria: CustomerId: {CustomerId}, Status: {Status}, DateRange: {StartDate} to {EndDate}",
                searchCriteria.CustomerId, searchCriteria.Status, searchCriteria.StartDate, searchCriteria.EndDate);

            try
            {
                // Build predicate expression for database-level filtering
                Expression<Func<OrderEntity, bool>> predicate = BuildSearchPredicate(searchCriteria);

                // Use filtered query instead of loading all orders
                var filteredOrders = await _unitOfWork.Orders.GetFilteredOrdersAsync(predicate, cancellationToken);

                // Apply sorting
                IEnumerable<OrderEntity> sorted = searchCriteria.SortBy?.ToLower() switch
                {
                    "totalamount" => searchCriteria.SortDescending ?
                        filteredOrders.OrderByDescending(o => o.TotalAmount) :
                        filteredOrders.OrderBy(o => o.TotalAmount),
                    "status" => searchCriteria.SortDescending ?
                        filteredOrders.OrderByDescending(o => o.Status) :
                        filteredOrders.OrderBy(o => o.Status),
                    _ => searchCriteria.SortDescending ?
                        filteredOrders.OrderByDescending(o => o.Date) :
                        filteredOrders.OrderBy(o => o.Date)
                };

                var totalCount = sorted.Count();

                // Apply pagination (Note: Still in-memory. Ideally should be in repository with IQueryable)
                var pagedOrders = sorted
                    .Skip((searchCriteria.PageNumber - 1) * searchCriteria.PageSize)
                    .Take(searchCriteria.PageSize)
                    .ToList();

                var orderResponses = pagedOrders.Select(o => o.ToOrderResponse()).ToList();

                var result = new PagedResponse<OrderResponse>
                {
                    Items = orderResponses,
                    TotalCount = totalCount,
                    PageNumber = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize
                };

                _logger.LogDebug("Successfully searched orders. Found {TotalCount} orders, returning page {PageNumber} with {ItemCount} items",
                    totalCount, searchCriteria.PageNumber, orderResponses.Count);

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search orders");
                throw new InvalidOperationException($"Failed to search orders: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Builds a predicate expression for order search filtering
        /// This allows database-level filtering instead of in-memory filtering
        /// </summary>
        private static Expression<Func<OrderEntity, bool>> BuildSearchPredicate(OrderSearchCriteria searchCriteria)
        {
            // Build a single combined predicate expression
            return o =>
                (!searchCriteria.CustomerId.HasValue || o.CustomerId == searchCriteria.CustomerId.Value) &&
                (!searchCriteria.Status.HasValue || o.Status == searchCriteria.Status.Value.ToString()) &&
                (!searchCriteria.StartDate.HasValue || o.Date >= searchCriteria.StartDate.Value) &&
                (!searchCriteria.EndDate.HasValue || o.Date <= searchCriteria.EndDate.Value) &&
                (!searchCriteria.MinAmount.HasValue || o.TotalAmount >= searchCriteria.MinAmount.Value) &&
                (!searchCriteria.MaxAmount.HasValue || o.TotalAmount <= searchCriteria.MaxAmount.Value);
        }
    }
}
