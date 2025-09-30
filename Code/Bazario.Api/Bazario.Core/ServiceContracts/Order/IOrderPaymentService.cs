using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Order;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Service contract for order payment processing
    /// Handles payment processing and related operations
    /// </summary>
    public interface IOrderPaymentService
    {
        /// <summary>
        /// Processes order payment and updates status accordingly
        /// </summary>
        /// <param name="orderId">Order ID to process payment for</param>
        /// <param name="paymentDetails">Payment processing details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment processing result</returns>
        Task<PaymentResult> ProcessOrderPaymentAsync(Guid orderId, PaymentDetails paymentDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refunds an order payment
        /// </summary>
        /// <param name="orderId">Order ID to refund</param>
        /// <param name="reason">Refund reason</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Refund result</returns>
        Task<PaymentResult> RefundOrderPaymentAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);
    }
}
