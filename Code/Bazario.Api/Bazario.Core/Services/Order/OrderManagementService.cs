using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Extensions;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order management operations (CRUD)
    /// Handles order creation, updates, cancellation, and deletion
    /// </summary>
    public class OrderManagementService : IOrderManagementService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderValidationService _validationService;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(
            IOrderRepository orderRepository,
            IOrderValidationService validationService,
            ILogger<OrderManagementService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponse> CreateOrderAsync(OrderAddRequest orderAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new order for customer: {CustomerId}", orderAddRequest?.CustomerId);

            try
            {
                if (orderAddRequest == null)
                {
                    throw new ArgumentNullException(nameof(orderAddRequest));
                }

                // Convert DTO to entity
                var order = orderAddRequest.ToOrder();
                order.OrderId = Guid.NewGuid();
                order.Date = DateTime.UtcNow;

                // Add order to database
                var createdOrder = await _orderRepository.AddOrderAsync(order, cancellationToken);

                _logger.LogInformation("Successfully created order: {OrderId} for customer: {CustomerId}",
                    createdOrder.OrderId, createdOrder.CustomerId);

                return createdOrder.ToOrderResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for customer: {CustomerId}", orderAddRequest?.CustomerId);
                throw;
            }
        }

        public async Task<OrderResponse> UpdateOrderAsync(OrderUpdateRequest orderUpdateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating order: {OrderId}", orderUpdateRequest?.OrderId);

            try
            {
                if (orderUpdateRequest == null)
                {
                    throw new ArgumentNullException(nameof(orderUpdateRequest));
                }

                // Check if order can be modified
                var canModify = await _validationService.CanOrderBeModifiedAsync(orderUpdateRequest.OrderId, cancellationToken);
                if (!canModify)
                {
                    throw new InvalidOperationException($"Order {orderUpdateRequest.OrderId} cannot be modified in its current state");
                }

                // Get existing order
                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderUpdateRequest.OrderId, cancellationToken);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {orderUpdateRequest.OrderId} not found");
                }

                // Update order properties
                existingOrder.TotalAmount = orderUpdateRequest.TotalAmount ?? existingOrder.TotalAmount;
                existingOrder.Status = orderUpdateRequest.Status.ToString();

                // Save changes
                var updatedOrder = await _orderRepository.UpdateOrderAsync(existingOrder, cancellationToken);

                _logger.LogInformation("Successfully updated order: {OrderId}", updatedOrder.OrderId);

                return updatedOrder.ToOrderResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order: {OrderId}", orderUpdateRequest?.OrderId);
                throw;
            }
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating order status: {OrderId} to {NewStatus}", orderId, newStatus);

            try
            {
                // Get existing order
                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {orderId} not found");
                }

                // Validate status transition
                var isValidTransition = _validationService.IsValidStatusTransition(existingOrder.Status ?? "", newStatus.ToString());
                if (!isValidTransition)
                {
                    throw new InvalidOperationException($"Invalid status transition from {existingOrder.Status} to {newStatus}");
                }

                // Update status
                existingOrder.Status = newStatus.ToString();

                // Save changes
                var updatedOrder = await _orderRepository.UpdateOrderAsync(existingOrder, cancellationToken);

                _logger.LogInformation("Successfully updated order status: {OrderId} to {NewStatus}", orderId, newStatus);

                return updatedOrder.ToOrderResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order status: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling order: {OrderId}", orderId);

            try
            {
                // Check if order can be cancelled
                var canCancel = await _validationService.CanOrderBeCancelledAsync(orderId, cancellationToken);
                if (!canCancel)
                {
                    throw new InvalidOperationException($"Order {orderId} cannot be cancelled in its current state");
                }

                // Update order status to Cancelled
                await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled, cancellationToken);

                _logger.LogInformation("Successfully cancelled order: {OrderId}", orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting order: {OrderId}", orderId);

            try
            {
                var result = await _orderRepository.DeleteOrderByIdAsync(orderId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully deleted order: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("Order not found for deletion: {OrderId}", orderId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete order: {OrderId}", orderId);
                throw;
            }
        }
    }
}
