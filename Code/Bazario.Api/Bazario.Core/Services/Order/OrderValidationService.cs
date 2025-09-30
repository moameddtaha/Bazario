using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Models.Order;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order validation and business rules
    /// Handles order validation, business rule checks, and calculations
    /// </summary>
    public class OrderValidationService : IOrderValidationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<OrderValidationService> _logger;

        public OrderValidationService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            ILogger<OrderValidationService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if order can be modified: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return false;
                }

                // Orders can only be modified if they are in Pending status
                var canModify = order.Status == OrderStatus.Pending.ToString();

                _logger.LogDebug("Order {OrderId} can be modified: {CanModify}", orderId, canModify);

                return canModify;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be modified: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CanOrderBeCancelledAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if order can be cancelled: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return false;
                }

                // Orders can be cancelled if they are Pending or Processing
                var canCancel = order.Status == OrderStatus.Pending.ToString() ||
                               order.Status == OrderStatus.Processing.ToString();

                _logger.LogDebug("Order {OrderId} can be cancelled: {CanCancel}", orderId, canCancel);

                return canCancel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if order can be cancelled: {OrderId}", orderId);
                throw;
            }
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            _logger.LogDebug("Validating status transition from {CurrentStatus} to {NewStatus}", currentStatus, newStatus);

            // Define valid status transitions
            var validTransitions = new Dictionary<string, List<string>>
            {
                { OrderStatus.Pending.ToString(), new List<string> { OrderStatus.Processing.ToString(), OrderStatus.Cancelled.ToString() } },
                { OrderStatus.Processing.ToString(), new List<string> { OrderStatus.Shipped.ToString(), OrderStatus.Cancelled.ToString() } },
                { OrderStatus.Shipped.ToString(), new List<string> { OrderStatus.Delivered.ToString() } },
                { OrderStatus.Delivered.ToString(), new List<string>() }, // Terminal state
                { OrderStatus.Cancelled.ToString(), new List<string>() }  // Terminal state
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                _logger.LogWarning("Unknown current status: {CurrentStatus}", currentStatus);
                return false;
            }

            var isValid = validTransitions[currentStatus].Contains(newStatus);

            _logger.LogDebug("Status transition from {CurrentStatus} to {NewStatus} is valid: {IsValid}",
                currentStatus, newStatus, isValid);

            return isValid;
        }

        public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(List<OrderItemAddRequest> orderItems, Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Calculating order total for {ItemCount} items, customer: {CustomerId}",
                orderItems?.Count ?? 0, customerId);

            try
            {
                if (orderItems == null || !orderItems.Any())
                {
                    throw new ArgumentException("Order items cannot be null or empty", nameof(orderItems));
                }

                decimal subtotal = 0;

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product {item.ProductId} not found");
                    }

                    subtotal += product.Price * item.Quantity;
                }

                // Calculate tax (10% for example)
                var tax = subtotal * 0.10m;

                // Calculate total
                var total = subtotal + tax;

                var calculation = new OrderTotalCalculation
                {
                    Subtotal = subtotal,
                    TaxAmount = tax,
                    DiscountAmount = 0,
                    ShippingCost = 0,
                    Total = total,
                    AppliedDiscounts = new List<string>()
                };

                _logger.LogDebug("Order total calculated: Subtotal: {Subtotal}, Tax: {Tax}, Total: {Total}",
                    subtotal, tax, total);

                return calculation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate order total");
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailabilityAsync(List<OrderItemAddRequest> orderItems, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating stock availability for {ItemCount} items", orderItems?.Count ?? 0);

            try
            {
                if (orderItems == null || !orderItems.Any())
                {
                    return true;
                }

                foreach (var item in orderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        _logger.LogWarning("Product not found: {ProductId}", item.ProductId);
                        return false;
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product: {ProductId}. Required: {Required}, Available: {Available}",
                            item.ProductId, item.Quantity, product.StockQuantity);
                        return false;
                    }
                }

                _logger.LogDebug("Stock availability validation passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate stock availability");
                throw;
            }
        }
    }
}
