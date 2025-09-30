using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using OrderEntity = Bazario.Core.Domain.Entities.Order;

namespace Bazario.Core.Helpers.Order
{
    /// <summary>
    /// Helper interface for order metrics calculations
    /// </summary>
    public interface IOrderMetricsHelper
    {
        /// <summary>
        /// Calculates the average processing time for a list of orders
        /// </summary>
        decimal CalculateAverageProcessingTime(List<OrderEntity> orders);

        /// <summary>
        /// Calculates the average delivery time for a list of orders
        /// </summary>
        decimal CalculateAverageDeliveryTime(List<OrderEntity> orders);

        /// <summary>
        /// Calculates the processing time for a single order
        /// </summary>
        decimal CalculateOrderProcessingTime(OrderEntity order);

        /// <summary>
        /// Calculates the delivery time for a single order
        /// </summary>
        decimal CalculateOrderDeliveryTime(OrderEntity order);
    }
}
