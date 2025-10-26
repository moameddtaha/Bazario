using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Models.Order;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order payment processing
    /// Handles COD (Cash on Delivery) payment processing and refunds
    /// Currently supports only cash payments - online payment gateway integration pending
    /// </summary>
    public class OrderPaymentService : IOrderPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderPaymentService> _logger;
        private const int RefundPeriodDays = 30; // Maximum days for refund eligibility

        public OrderPaymentService(
            IUnitOfWork unitOfWork,
            ILogger<OrderPaymentService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PaymentResult> ProcessOrderPaymentAsync(Guid orderId, PaymentDetails paymentDetails, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            if (paymentDetails == null)
            {
                throw new ArgumentNullException(nameof(paymentDetails), "Payment details cannot be null");
            }

            if (string.IsNullOrWhiteSpace(paymentDetails.PaymentMethod))
            {
                throw new ArgumentException("Payment method is required", nameof(paymentDetails));
            }

            if (!paymentDetails.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Only Cash on Delivery (COD) payments are supported. Provided: {paymentDetails.PaymentMethod}",
                    nameof(paymentDetails));
            }

            if (paymentDetails.Amount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than 0", nameof(paymentDetails));
            }

            _logger.LogInformation("Processing COD payment for order: {OrderId}, Amount: {Amount}", orderId, paymentDetails.Amount);

            try
            {
                var order = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Validate payment amount matches order total
                if (paymentDetails.Amount != order.TotalAmount)
                {
                    throw new ArgumentException(
                        $"Payment amount ({paymentDetails.Amount:C}) does not match order total ({order.TotalAmount:C})",
                        nameof(paymentDetails));
                }

                // Process COD payment - record cash payment received
                var result = new PaymentResult
                {
                    IsSuccessful = true,
                    TransactionId = Guid.NewGuid().ToString(),
                    ProcessedAmount = order.TotalAmount,
                    ErrorMessage = null
                };

                _logger.LogInformation("COD payment processed successfully for order: {OrderId}, TransactionId: {TransactionId}, Amount: {Amount}",
                    orderId, result.TransactionId, result.ProcessedAmount);

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw business rule exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process COD payment for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to process payment for order {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<PaymentResult> RefundOrderPaymentAsync(Guid orderId, decimal refundAmount, string reason, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            if (refundAmount <= 0)
            {
                throw new ArgumentException("Refund amount must be greater than 0", nameof(refundAmount));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Refund reason is required", nameof(reason));
            }

            _logger.LogInformation("Processing cash refund for order: {OrderId}, Amount: {RefundAmount}, Reason: {Reason}",
                orderId, refundAmount, reason);

            try
            {
                var order = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Business Rule 1: Validate refund amount does not exceed order total
                if (refundAmount > order.TotalAmount)
                {
                    throw new ArgumentException(
                        $"Refund amount ({refundAmount:C}) cannot exceed order total ({order.TotalAmount:C})",
                        nameof(refundAmount));
                }

                // Business Rule 2: Order must be in refundable status
                if (order.Status != "Delivered" && order.Status != "Completed")
                {
                    throw new InvalidOperationException(
                        $"Order cannot be refunded. Current status: {order.Status}. " +
                        "Only Delivered or Completed orders can be refunded.");
                }

                // Business Rule 3: Refund must be within allowed time period (30 days)
                var daysSinceOrder = (DateTime.UtcNow - order.Date).Days;
                if (daysSinceOrder > RefundPeriodDays)
                {
                    throw new InvalidOperationException(
                        $"Refund period expired. Orders can only be refunded within {RefundPeriodDays} days. " +
                        $"This order was placed {daysSinceOrder} days ago.");
                }

                // Process cash refund
                var result = new PaymentResult
                {
                    IsSuccessful = true,
                    TransactionId = Guid.NewGuid().ToString(),
                    ProcessedAmount = refundAmount,
                    ErrorMessage = null
                };

                // Update order status to Refunded
                order.Status = "Refunded";
                await _unitOfWork.Orders.UpdateOrderAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cash refund processed successfully for order: {OrderId}, TransactionId: {TransactionId}, Amount: {RefundAmount}, Order status updated to Refunded",
                    orderId, result.TransactionId, refundAmount);

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw business rule exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process refund for order: {OrderId}", orderId);
                throw new InvalidOperationException($"Failed to process refund for order {orderId}: {ex.Message}", ex);
            }
        }
    }
}
