using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Extensions.Order;
using Bazario.Core.Helpers.Authorization;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Service implementation for order management operations (CRUD)
    /// Handles order creation, updates, cancellation, and deletion
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class OrderManagementService : IOrderManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderValidationService _validationService;
        private readonly IAdminAuthorizationHelper _adminAuthHelper;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(
            IUnitOfWork unitOfWork,
            IOrderValidationService validationService,
            IAdminAuthorizationHelper adminAuthHelper,
            ILogger<OrderManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _adminAuthHelper = adminAuthHelper ?? throw new ArgumentNullException(nameof(adminAuthHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponse> CreateOrderAsync(OrderAddRequest orderAddRequest, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (orderAddRequest == null)
            {
                throw new ArgumentNullException(nameof(orderAddRequest));
            }

            _logger.LogInformation("Creating new order for customer: {CustomerId}", orderAddRequest.CustomerId);

            try
            {

                // ✅ VALIDATE STOCK AVAILABILITY BEFORE CREATING ORDER
                var stockValidation = await _validationService.ValidateStockAvailabilityWithDetailsAsync(
                    orderAddRequest.OrderItems,
                    cancellationToken);

                if (!stockValidation.IsValid)
                {
                    _logger.LogWarning("Order creation failed due to stock availability issues for customer: {CustomerId}. {Message}",
                        orderAddRequest.CustomerId, stockValidation.Message);

                    // Create detailed error message
                    var errorDetails = string.Join("; ", stockValidation.InvalidItems.Select(i => i.ErrorMessage));
                    throw new InvalidOperationException(
                        $"Cannot create order: {stockValidation.Message}. Details: {errorDetails}");
                }

                _logger.LogDebug("Stock validation passed for all {ItemCount} items", orderAddRequest.OrderItems.Count);

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
                var createdOrder = await _unitOfWork.Orders.AddOrderAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            // Validate inputs
            if (orderUpdateRequest == null)
            {
                throw new ArgumentNullException(nameof(orderUpdateRequest));
            }

            if (orderUpdateRequest.OrderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderUpdateRequest));
            }

            _logger.LogInformation("Updating order: {OrderId}", orderUpdateRequest.OrderId);

            try
            {
                // Check if order can be modified
                var canModify = await _validationService.CanOrderBeModifiedAsync(orderUpdateRequest.OrderId, cancellationToken);
                if (!canModify)
                {
                    throw new InvalidOperationException($"Order {orderUpdateRequest.OrderId} cannot be modified in its current state");
                }

                // Get existing order
                var existingOrder = await _unitOfWork.Orders.GetOrderByIdAsync(orderUpdateRequest.OrderId, cancellationToken);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {orderUpdateRequest.OrderId} not found");
                }

                // Business validation for new properties (delegated to validation service for KISS)
                _validationService.ValidateOrderUpdateBusinessRules(orderUpdateRequest, existingOrder);

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
                var updatedOrder = await _unitOfWork.Orders.UpdateOrderAsync(existingOrder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            _logger.LogInformation("Updating order status: {OrderId} to {NewStatus}", orderId, newStatus);

            try
            {
                // Get existing order
                var existingOrder = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
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
                var updatedOrder = await _unitOfWork.Orders.UpdateOrderAsync(existingOrder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            // Validate inputs
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

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

        public async Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Soft deleting order: {OrderId}, DeletedBy: {DeletedBy}, Reason: {Reason}", orderId, deletedBy, reason);

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

                // Check if order exists
                var existingOrder = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for soft deletion", orderId);
                    return false;
                }

                _logger.LogDebug("Soft deleting order {OrderId}", orderId);

                // Perform soft delete
                var result = await _unitOfWork.Orders.SoftDeleteOrderAsync(orderId, deletedBy, reason, cancellationToken);

                if (result)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully soft deleted order: {OrderId} by user: {DeletedBy}", orderId, deletedBy);
                }
                else
                {
                    _logger.LogWarning("Failed to soft delete order: {OrderId}", orderId);
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while soft deleting order: {OrderId}", orderId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> HardDeleteOrderAsync(Guid orderId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
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

                // Validate admin privileges
                await _adminAuthHelper.ValidateAdminPrivilegesAsync(deletedBy, cancellationToken);

                // Check if order exists before deletion
                var existingOrder = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for deletion", orderId);
                    return false;
                }

                _logger.LogInformation("Admin user {UserId} performing hard delete of order {OrderId}", deletedBy, orderId);

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. OrderId: {OrderId}, DeletedBy: {DeletedBy}, Reason: {Reason}", 
                    orderId, deletedBy, reason);

                var result = await _unitOfWork.Orders.DeleteOrderByIdAsync(orderId, cancellationToken);

                if (result)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
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

        public async Task<OrderResponse> RestoreOrderAsync(Guid orderId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Restoring soft deleted order: {OrderId}, RestoredBy: {RestoredBy}", orderId, restoredBy);

            try
            {
                // Validate inputs
                if (orderId == Guid.Empty)
                {
                    throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
                }

                if (restoredBy == Guid.Empty)
                {
                    throw new ArgumentException("RestoredBy user ID cannot be empty", nameof(restoredBy));
                }

                // Check if soft-deleted order exists
                var existingOrder = await _unitOfWork.Orders.GetOrderByIdIncludeDeletedAsync(orderId, cancellationToken);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {orderId} not found");
                }

                if (!existingOrder.IsDeleted)
                {
                    throw new InvalidOperationException($"Order {orderId} is not soft deleted, cannot restore");
                }

                _logger.LogDebug("Restoring order {OrderId}", orderId);

                // Perform restore
                var result = await _unitOfWork.Orders.RestoreOrderAsync(orderId, cancellationToken);

                if (!result)
                {
                    throw new InvalidOperationException($"Failed to restore order {orderId}");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully restored order: {OrderId} by user: {RestoredBy}", orderId, restoredBy);

                // Get the restored order and return
                var restoredOrder = await _unitOfWork.Orders.GetOrderByIdAsync(orderId, cancellationToken);
                if (restoredOrder == null)
                {
                    throw new InvalidOperationException($"Failed to retrieve restored order {orderId}");
                }

                return restoredOrder.ToOrderResponse();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while restoring order: {OrderId}", orderId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error while restoring order: {OrderId}", orderId);
                throw; // Re-throw business logic exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore order: {OrderId}", orderId);
                throw;
            }
        }

    }
}
