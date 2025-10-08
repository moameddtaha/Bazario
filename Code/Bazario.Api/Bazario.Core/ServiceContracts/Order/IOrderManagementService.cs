using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order management operations (CRUD)
    /// Handles order creation, updates, cancellation, and deletion
    /// </summary>
    public interface IOrderManagementService
    {
        /// <summary>
        /// Creates a new order with validation and business rules
        /// </summary>
        /// <param name="orderAddRequest">Order creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created order response</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderAddRequest is null</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        Task<OrderResponse> CreateOrderAsync(OrderAddRequest orderAddRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing order with business rule validation
        /// </summary>
        /// <param name="orderUpdateRequest">Order update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated order response</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderUpdateRequest is null</exception>
        /// <exception cref="OrderNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="OrderUpdateNotAllowedException">Thrown when order cannot be updated due to status</exception>
        Task<OrderResponse> UpdateOrderAsync(OrderUpdateRequest orderUpdateRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates order status with business rule validation
        /// </summary>
        /// <param name="orderId">Order ID to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated order response</returns>
        /// <exception cref="OrderNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="InvalidStatusTransitionException">Thrown when status transition is not valid</exception>
        Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an order if business rules allow
        /// </summary>
        /// <param name="orderId">Order ID to cancel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully cancelled</returns>
        /// <exception cref="OrderNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="OrderCancellationNotAllowedException">Thrown when order cannot be cancelled</exception>
        Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hard deletes an order (completely removes from database) - ADMIN ONLY
        /// </summary>
        /// <param name="orderId">Order ID to delete</param>
        /// <param name="deletedBy">Admin user ID performing the deletion</param>
        /// <param name="reason">Reason for deletion (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not an admin</exception>
        /// <exception cref="OrderNotFoundException">Thrown when order is not found</exception>
        Task<bool> DeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default);
    }
}
