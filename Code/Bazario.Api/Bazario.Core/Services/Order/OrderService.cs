using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Order;
using Bazario.Core.Enums.Order;
using Bazario.Core.Models.Catalog.Discount;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Shared;
using Bazario.Core.ServiceContracts.Order;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Order
{
    /// <summary>
    /// Composite service implementation for order operations
    /// Delegates to specialized services while providing a unified interface
    /// Follows Single Responsibility Principle by delegating to specialized services
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrderManagementService _managementService;
        private readonly IOrderQueryService _queryService;
        private readonly IOrderValidationService _validationService;
        private readonly IOrderAnalyticsService _analyticsService;
        private readonly IOrderPaymentService _paymentService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderManagementService managementService,
            IOrderQueryService queryService,
            IOrderValidationService validationService,
            IOrderAnalyticsService analyticsService,
            IOrderPaymentService paymentService,
            ILogger<OrderService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // IOrderManagementService methods
        public async Task<OrderResponse> CreateOrderAsync(OrderAddRequest orderAddRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CreateOrderAsync to OrderManagementService");
            return await _managementService.CreateOrderAsync(orderAddRequest, cancellationToken);
        }

        public async Task<OrderResponse> UpdateOrderAsync(OrderUpdateRequest orderUpdateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating UpdateOrderAsync to OrderManagementService");
            return await _managementService.UpdateOrderAsync(orderUpdateRequest, cancellationToken);
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating UpdateOrderStatusAsync to OrderManagementService");
            return await _managementService.UpdateOrderStatusAsync(orderId, newStatus, cancellationToken);
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CancelOrderAsync to OrderManagementService");
            return await _managementService.CancelOrderAsync(orderId, cancellationToken);
        }

        public async Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating SoftDeleteOrderAsync to OrderManagementService");
            return await _managementService.SoftDeleteOrderAsync(orderId, deletedBy, reason, cancellationToken);
        }

        public async Task<bool> HardDeleteOrderAsync(Guid orderId, Guid deletedBy, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating HardDeleteOrderAsync to OrderManagementService");
            return await _managementService.HardDeleteOrderAsync(orderId, deletedBy, reason, cancellationToken);
        }

        public async Task<OrderResponse> RestoreOrderAsync(Guid orderId, Guid restoredBy, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating RestoreOrderAsync to OrderManagementService");
            return await _managementService.RestoreOrderAsync(orderId, restoredBy, cancellationToken);
        }

        // IOrderQueryService methods
        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetOrderByIdAsync to OrderQueryService");
            return await _queryService.GetOrderByIdAsync(orderId, cancellationToken);
        }

        public async Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetOrdersByCustomerIdAsync to OrderQueryService");
            return await _queryService.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
        }

        public async Task<PagedResponse<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetOrdersByStatusAsync to OrderQueryService");
            return await _queryService.GetOrdersByStatusAsync(status, pageNumber, pageSize, cancellationToken);
        }

        public async Task<PagedResponse<OrderResponse>> SearchOrdersAsync(OrderSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating SearchOrdersAsync to OrderQueryService");
            return await _queryService.SearchOrdersAsync(searchCriteria, cancellationToken);
        }

        // IOrderValidationService methods
        public async Task<bool> CanOrderBeModifiedAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CanOrderBeModifiedAsync to OrderValidationService");
            return await _validationService.CanOrderBeModifiedAsync(orderId, cancellationToken);
        }

        public async Task<bool> CanOrderBeCancelledAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CanOrderBeCancelledAsync to OrderValidationService");
            return await _validationService.CanOrderBeCancelledAsync(orderId, cancellationToken);
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            _logger.LogDebug("Delegating IsValidStatusTransition to OrderValidationService");
            return _validationService.IsValidStatusTransition(currentStatus, newStatus);
        }

        public async Task<OrderTotalCalculation> CalculateOrderTotalAsync(List<OrderItemAddRequest> orderItems, Guid customerId, ShippingAddress shippingAddress, List<string>? discountCodes = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating CalculateOrderTotalAsync to OrderValidationService");
            return await _validationService.CalculateOrderTotalAsync(orderItems, customerId, shippingAddress, discountCodes, cancellationToken);
        }

        public async Task<StockValidationResult> ValidateStockAvailabilityWithDetailsAsync(List<OrderItemAddRequest> orderItems, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating ValidateStockAvailabilityWithDetailsAsync to OrderValidationService");
            return await _validationService.ValidateStockAvailabilityWithDetailsAsync(orderItems, cancellationToken);
        }

        public void ValidateOrderUpdateBusinessRules(OrderUpdateRequest orderUpdateRequest, Domain.Entities.Order.Order existingOrder)
        {
            _logger.LogDebug("Delegating ValidateOrderUpdateBusinessRules to OrderValidationService");
            _validationService.ValidateOrderUpdateBusinessRules(orderUpdateRequest, existingOrder);
        }

        // IOrderAnalyticsService methods
        public async Task<CustomerOrderAnalytics> GetCustomerOrderAnalyticsAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetCustomerOrderAnalyticsAsync to OrderAnalyticsService");
            return await _analyticsService.GetCustomerOrderAnalyticsAsync(customerId, cancellationToken);
        }

        public async Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetRevenueAnalyticsAsync to OrderAnalyticsService");
            return await _analyticsService.GetRevenueAnalyticsAsync(startDate, endDate, cancellationToken);
        }

        public async Task<OrderPerformanceMetrics> GetOrderPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetOrderPerformanceMetricsAsync to OrderAnalyticsService");
            return await _analyticsService.GetOrderPerformanceMetricsAsync(startDate, endDate, cancellationToken);
        }

        // IOrderPaymentService methods
        public async Task<PaymentResult> ProcessOrderPaymentAsync(Guid orderId, PaymentDetails paymentDetails, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating ProcessOrderPaymentAsync to OrderPaymentService");
            return await _paymentService.ProcessOrderPaymentAsync(orderId, paymentDetails, cancellationToken);
        }

        public async Task<PaymentResult> RefundOrderPaymentAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating RefundOrderPaymentAsync to OrderPaymentService");
            return await _paymentService.RefundOrderPaymentAsync(orderId, reason, cancellationToken);
        }

        // IOrderAnalyticsService discount methods
        public async Task<DiscountUsageStats?> GetDiscountUsageStatsAsync(string discountCode, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetDiscountUsageStatsAsync to OrderAnalyticsService");
            return await _analyticsService.GetDiscountUsageStatsAsync(discountCode, cancellationToken);
        }

        public async Task<List<DiscountPerformance>> GetDiscountPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetDiscountPerformanceAsync to OrderAnalyticsService");
            return await _analyticsService.GetDiscountPerformanceAsync(startDate, endDate, cancellationToken);
        }

        public async Task<DiscountRevenueImpact> GetDiscountRevenueImpactAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Delegating GetDiscountRevenueImpactAsync to OrderAnalyticsService");
            return await _analyticsService.GetDiscountRevenueImpactAsync(startDate, endDate, cancellationToken);
        }
    }
}
