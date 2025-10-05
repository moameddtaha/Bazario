using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums;
using Bazario.Core.Extensions;
using Bazario.Core.ServiceContracts.Order;
using Bazario.Core.Helpers.Product;
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
        private readonly IProductValidationHelper _validationHelper;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(
            IOrderRepository orderRepository,
            IOrderValidationService validationService,
            IProductValidationHelper validationHelper,
            ILogger<OrderManagementService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
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

                // Check if order should be calculated (customer orders) or use provided values (admin orders)
                if (orderAddRequest.ShouldCalculateOrder())
                {
                    _logger.LogDebug("Calculating order total for customer order: {OrderId}", order.OrderId);
                    
                    // Calculate order total including shipping and discounts
                    var calculation = await _validationService.CalculateOrderTotalAsync(
                        orderAddRequest.OrderItems,
                        orderAddRequest.CustomerId,
                        orderAddRequest.ShippingAddress,
                        orderAddRequest.DiscountCodes,
                        cancellationToken);

                    // Populate order entity with calculated values
                    order.Subtotal = calculation.Subtotal;
                    order.DiscountAmount = calculation.DiscountAmount;
                    order.ShippingCost = calculation.ShippingCost;
                    order.TotalAmount = calculation.Total;
                    
                    // Set applied discount types from the calculation
                    if (calculation.AppliedDiscounts.Any())
                    {
                        // Extract discount types from AppliedDiscounts (format: "CODE (Type)")
                        var discountTypes = calculation.AppliedDiscounts
                            .Select(d => d.Contains('(') ? d.Split('(')[1].TrimEnd(')') : "Unknown")
                            .ToList();
                        order.AppliedDiscountTypes = string.Join(",", discountTypes);
                    }

                    _logger.LogDebug("Order calculated - Subtotal: {Subtotal}, Discount: {Discount}, Shipping: {Shipping}, Total: {Total}",
                        order.Subtotal, order.DiscountAmount, order.ShippingCost, order.TotalAmount);
                }
                else
                {
                    _logger.LogDebug("Using provided values for admin order: {OrderId}", order.OrderId);
                    // For admin orders, use the provided values from the request
                    // The order entity already has the values from ToOrder() method
                }

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

                // Business validation for new properties
                ValidateOrderUpdateBusinessRules(orderUpdateRequest, existingOrder);

                // Update order properties
                existingOrder.TotalAmount = orderUpdateRequest.TotalAmount ?? existingOrder.TotalAmount;
                existingOrder.Status = orderUpdateRequest.Status?.ToString() ?? existingOrder.Status;
                
                // Update discount and shipping properties if provided
                if (orderUpdateRequest.AppliedDiscountCodes != null)
                    existingOrder.AppliedDiscountCodes = orderUpdateRequest.AppliedDiscountCodes;
                if (orderUpdateRequest.DiscountAmount.HasValue)
                    existingOrder.DiscountAmount = orderUpdateRequest.DiscountAmount.Value;
                if (orderUpdateRequest.AppliedDiscountTypes != null)
                    existingOrder.AppliedDiscountTypes = orderUpdateRequest.AppliedDiscountTypes;
                if (orderUpdateRequest.ShippingCost.HasValue)
                    existingOrder.ShippingCost = orderUpdateRequest.ShippingCost.Value;
                if (orderUpdateRequest.Subtotal.HasValue)
                    existingOrder.Subtotal = orderUpdateRequest.Subtotal.Value;

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

        public async Task<bool> DeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin user {DeletedBy} attempting to delete order: {OrderId}, Reason: {Reason}", deletedBy, orderId, reason);

            try
            {
                // Validate inputs
                if (orderId == Guid.Empty)
                {
                    throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Reason is required for hard deletion", nameof(reason));
                }

                // Check if user has admin privileges
                if (!await _validationHelper.HasAdminPrivilegesAsync(deletedBy, cancellationToken))
                {
                    _logger.LogWarning("User {UserId} attempted hard delete of order {OrderId} without admin privileges", deletedBy, orderId);
                    throw new UnauthorizedAccessException("Only administrators can perform hard deletion of orders");
                }

                // Check if order exists before deletion
                var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for deletion", orderId);
                    return false;
                }

                _logger.LogInformation("Admin user {UserId} performing hard delete of order {OrderId}", deletedBy, orderId);

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. OrderId: {OrderId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                    orderId, deletedBy, reason);

                var result = await _orderRepository.DeleteOrderByIdAsync(orderId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully deleted order: {OrderId} by admin user: {DeletedBy}", orderId, deletedBy);
                }
                else
                {
                    _logger.LogWarning("Failed to delete order: {OrderId}", orderId);
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while deleting order: {OrderId}", orderId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Authorization error while deleting order: {OrderId}", orderId);
                throw; // Re-throw authorization exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete order: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Validates business rules for order updates
        /// </summary>
        private void ValidateOrderUpdateBusinessRules(OrderUpdateRequest orderUpdateRequest, Domain.Entities.Order existingOrder)
        {
            _logger.LogDebug("Validating business rules for order update: {OrderId}", orderUpdateRequest.OrderId);

            // Get the values that will be used for validation (either from request or existing order)
            var subtotal = orderUpdateRequest.Subtotal ?? existingOrder.Subtotal;
            var discountAmount = orderUpdateRequest.DiscountAmount ?? existingOrder.DiscountAmount;
            var shippingCost = orderUpdateRequest.ShippingCost ?? existingOrder.ShippingCost;
            var totalAmount = orderUpdateRequest.TotalAmount ?? existingOrder.TotalAmount;

            // Business Rule 1: Discount amount should not exceed subtotal
            if (discountAmount > subtotal)
            {
                _logger.LogWarning("Business rule violation: Discount amount {DiscountAmount} exceeds subtotal {Subtotal} for order {OrderId}",
                    discountAmount, subtotal, orderUpdateRequest.OrderId);
                throw new InvalidOperationException($"Discount amount ({discountAmount:C}) cannot exceed subtotal ({subtotal:C})");
            }

            // Business Rule 2: Total amount should be consistent with subtotal, discount, and shipping
            var expectedTotal = subtotal + shippingCost - discountAmount;
            if (Math.Abs(totalAmount - expectedTotal) > 0.01m) // Allow for small rounding differences
            {
                _logger.LogWarning("Business rule violation: Total amount {TotalAmount} does not match calculated total {ExpectedTotal} (Subtotal: {Subtotal}, Shipping: {ShippingCost}, Discount: {DiscountAmount}) for order {OrderId}",
                    totalAmount, expectedTotal, subtotal, shippingCost, discountAmount, orderUpdateRequest.OrderId);
                throw new InvalidOperationException($"Total amount ({totalAmount:C}) does not match calculated total ({expectedTotal:C}). Expected: Subtotal + Shipping - Discount = {subtotal:C} + {shippingCost:C} - {discountAmount:C}");
            }

            // Business Rule 3: AppliedDiscountCodes and AppliedDiscountTypes should be consistent
            if (!string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountCodes) && !string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountTypes))
            {
                var codeCount = orderUpdateRequest.AppliedDiscountCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                var typeCount = orderUpdateRequest.AppliedDiscountTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                
                if (codeCount != typeCount)
                {
                    _logger.LogWarning("Business rule violation: AppliedDiscountCodes count ({CodeCount}) does not match AppliedDiscountTypes count ({TypeCount}) for order {OrderId}",
                        codeCount, typeCount, orderUpdateRequest.OrderId);
                    throw new InvalidOperationException($"Applied discount codes count ({codeCount}) must match applied discount types count ({typeCount})");
                }
            }

            // Business Rule 4: If discount amount is provided, discount codes should also be provided
            if (orderUpdateRequest.DiscountAmount.HasValue && orderUpdateRequest.DiscountAmount.Value > 0 && 
                string.IsNullOrEmpty(orderUpdateRequest.AppliedDiscountCodes))
            {
                _logger.LogWarning("Business rule violation: Discount amount {DiscountAmount} provided but no discount codes for order {OrderId}",
                    orderUpdateRequest.DiscountAmount.Value, orderUpdateRequest.OrderId);
                throw new InvalidOperationException("Discount amount cannot be greater than 0 without applied discount codes");
            }

            _logger.LogDebug("Business rules validation passed for order update: {OrderId}", orderUpdateRequest.OrderId);
        }
    }
}
