using System;

namespace Bazario.Core.ServiceContracts.Order
{
    /// <summary>
    /// Composite service contract for order operations
    /// Combines all specialized order services into a single interface
    /// Follows Interface Segregation Principle by inheriting from specialized interfaces
    /// </summary>
    public interface IOrderService : 
        IOrderManagementService, 
        IOrderQueryService, 
        IOrderValidationService, 
        IOrderAnalyticsService, 
        IOrderPaymentService
    {
        // This interface inherits all methods from the specialized interfaces:
        // - IOrderManagementService: CreateOrderAsync, UpdateOrderAsync, UpdateOrderStatusAsync, CancelOrderAsync, DeleteOrderAsync
        // - IOrderQueryService: GetOrderByIdAsync, GetOrdersByCustomerIdAsync, GetOrdersByStatusAsync, SearchOrdersAsync
        // - IOrderValidationService: CanOrderBeModifiedAsync, CanOrderBeCancelledAsync, IsValidStatusTransition, CalculateOrderTotalAsync, ValidateStockAvailabilityAsync
        // - IOrderAnalyticsService: GetCustomerOrderAnalyticsAsync, GetRevenueAnalyticsAsync, GetOrderPerformanceMetricsAsync
        // - IOrderPaymentService: ProcessOrderPaymentAsync, RefundOrderPaymentAsync
    }
}
