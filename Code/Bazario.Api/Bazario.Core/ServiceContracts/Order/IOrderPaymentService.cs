using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order payment processing
    /// Handles COD (Cash on Delivery) payment processing and refunds
    /// Currently supports only cash payments - online payment gateway integration pending
    /// </summary>
    public interface IOrderPaymentService
    {
        /// <summary>
        /// Processes a COD (Cash on Delivery) payment for an order
        /// </summary>
        /// <param name="orderId">Order ID to process payment for</param>
        /// <param name="paymentDetails">Payment details (must specify PaymentMethod = "COD")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment processing result with transaction details</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails or non-COD payment method provided</exception>
        /// <exception cref="InvalidOperationException">Thrown when order not found or payment processing fails</exception>
        /// <remarks>
        /// Only Cash on Delivery (COD) payments are currently supported.
        /// Payment amount must match the order total exactly.
        /// </remarks>
        Task<PaymentResult> ProcessOrderPaymentAsync(Guid orderId, PaymentDetails paymentDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a refund for an order (supports partial refunds)
        /// </summary>
        /// <param name="orderId">Order ID to refund</param>
        /// <param name="refundAmount">Amount to refund (can be partial, must not exceed order total)</param>
        /// <param name="reason">Refund reason (required)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Refund result with transaction details</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules are violated or refund processing fails</exception>
        /// <remarks>
        /// Business rules:
        /// - Order must be in "Delivered" or "Completed" status (ensures order has been paid)
        /// - Refund must be within 30 days of order date
        /// - Refund amount must be greater than 0 and not exceed order total
        /// - Order status will be updated to "Refunded" after successful refund
        /// </remarks>
        Task<PaymentResult> RefundOrderPaymentAsync(Guid orderId, decimal refundAmount, string reason, CancellationToken cancellationToken = default);
    }
}
