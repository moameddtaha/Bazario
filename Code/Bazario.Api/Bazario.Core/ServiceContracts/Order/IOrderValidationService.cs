using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.Models.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order validation and business rules
    /// Handles order validation, business rule checks, and calculations
    /// </summary>
    public interface IOrderValidationService
    {
        /// <summary>
        /// Validates if an order can be modified based on current status
        /// </summary>
        /// <param name="orderId">Order ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if order can be modified</returns>
        Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an order can be cancelled based on current status
        /// </summary>
        /// <param name="orderId">Order ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if order can be cancelled</returns>
        Task<bool> CanOrderBeCancelledAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a status transition is allowed
        /// </summary>
        /// <param name="currentStatus">Current order status</param>
        /// <param name="newStatus">New status to transition to</param>
        /// <returns>True if transition is valid</returns>
        bool IsValidStatusTransition(string currentStatus, string newStatus);

        /// <summary>
        /// Calculates order total including shipping costs and discounts
        /// </summary>
        /// <param name="orderItems">List of order items</param>
        /// <param name="customerId">Customer ID for calculation</param>
        /// <param name="shippingAddress">Shipping address information</param>
        /// <param name="discountCode">Optional discount code to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order total calculation result</returns>
        Task<OrderTotalCalculation> CalculateOrderTotalAsync(List<OrderItemAddRequest> orderItems, Guid customerId, ShippingAddress shippingAddress, List<string>? discountCodes = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates order items have sufficient stock
        /// </summary>
        /// <param name="orderItems">List of order items to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all items have sufficient stock</returns>
        Task<bool> ValidateStockAvailabilityAsync(List<OrderItemAddRequest> orderItems, CancellationToken cancellationToken = default);
    }
}
