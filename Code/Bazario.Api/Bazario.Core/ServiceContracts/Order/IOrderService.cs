using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order management operations
    /// Handles order creation, updates, validation, and business logic
    /// </summary>
    public interface IOrderService
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
        /// Cancels an order if business rules allow
        /// </summary>
        /// <param name="orderId">Order ID to cancel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully cancelled</returns>
        /// <exception cref="OrderNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="OrderCancellationNotAllowedException">Thrown when order cannot be cancelled</exception>
        Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

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
        /// Processes order payment and updates status accordingly
        /// </summary>
        /// <param name="orderId">Order ID to process payment for</param>
        /// <param name="paymentDetails">Payment processing details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment processing result</returns>
        Task<PaymentResult> ProcessOrderPaymentAsync(Guid orderId, PaymentDetails paymentDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates order total including taxes and discounts
        /// </summary>
        /// <param name="orderItems">List of order items</param>
        /// <param name="customerId">Customer ID for tax calculation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order total calculation result</returns>
        Task<OrderTotalCalculation> CalculateOrderTotalAsync(List<OrderItemAddRequest> orderItems, Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an order can be modified based on current status
        /// </summary>
        /// <param name="orderId">Order ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if order can be modified</returns>
        Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default);
    }

}
