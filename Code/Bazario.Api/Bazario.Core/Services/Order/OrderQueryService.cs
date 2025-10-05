using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Extensions;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;
using OrderEntity = Bazario.Core.Domain.Entities.Order;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order query operations
    /// Handles order retrieval, searching, and filtering
    /// </summary>
    public class OrderQueryService : IOrderQueryService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderQueryService> _logger;

        public OrderQueryService(
            IOrderRepository orderRepository,
            ILogger<OrderQueryService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving order by ID: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                
                if (order == null)
                {
                    _logger.LogDebug("Order not found: {OrderId}", orderId);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved order: {OrderId}, Customer: {CustomerId}", 
                    order.OrderId, order.CustomerId);

                return order.ToOrderResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving orders for customer: {CustomerId}", customerId);

            try
            {
                var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
                
                _logger.LogDebug("Successfully retrieved {Count} orders for customer: {CustomerId}", 
                    orders.Count, customerId);

                return orders.Select(o => o.ToOrderResponse()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for customer: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<PagedResponse<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving orders by status: {Status}, Page: {PageNumber}, Size: {PageSize}", 
                status, pageNumber, pageSize);

            try
            {
                var allOrders = await _orderRepository.GetOrdersByStatusAsync(status, cancellationToken);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders by status: {Status}", status);
                throw;
            }
        }

        public async Task<PagedResponse<OrderResponse>> SearchOrdersAsync(OrderSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching orders with criteria: CustomerId: {CustomerId}, Status: {Status}, DateRange: {StartDate} to {EndDate}",
                searchCriteria.CustomerId, searchCriteria.Status, searchCriteria.StartDate, searchCriteria.EndDate);

            try
            {
                // Get all orders and apply filters (for now, until we add IQueryable to repository)
                var allOrders = await _orderRepository.GetAllOrdersAsync(cancellationToken);
                
                IEnumerable<OrderEntity> filtered = allOrders;

                // Apply filters
                if (searchCriteria.CustomerId.HasValue)
                {
                    filtered = filtered.Where(o => o.CustomerId == searchCriteria.CustomerId.Value);
                }

                if (searchCriteria.Status.HasValue)
                {
                    filtered = filtered.Where(o => o.Status == searchCriteria.Status.Value.ToString());
                }

                if (searchCriteria.StartDate.HasValue)
                {
                    filtered = filtered.Where(o => o.Date >= searchCriteria.StartDate.Value);
                }

                if (searchCriteria.EndDate.HasValue)
                {
                    filtered = filtered.Where(o => o.Date <= searchCriteria.EndDate.Value);
                }

                if (searchCriteria.MinAmount.HasValue)
                {
                    filtered = filtered.Where(o => o.TotalAmount >= searchCriteria.MinAmount.Value);
                }

                if (searchCriteria.MaxAmount.HasValue)
                {
                    filtered = filtered.Where(o => o.TotalAmount <= searchCriteria.MaxAmount.Value);
                }

                // Apply sorting
                filtered = searchCriteria.SortBy?.ToLower() switch
                {
                    "totalamount" => searchCriteria.SortDescending ?
                        filtered.OrderByDescending(o => o.TotalAmount) :
                        filtered.OrderBy(o => o.TotalAmount),
                    "status" => searchCriteria.SortDescending ?
                        filtered.OrderByDescending(o => o.Status) :
                        filtered.OrderBy(o => o.Status),
                    _ => searchCriteria.SortDescending ?
                        filtered.OrderByDescending(o => o.Date) :
                        filtered.OrderBy(o => o.Date)
                };

                var totalCount = filtered.Count();

                // Apply pagination
                var pagedOrders = filtered
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search orders");
                throw;
            }
        }
    }
}
