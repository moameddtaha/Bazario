using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Models.Inventory;
using Bazario.Core.ServiceContracts.Inventory;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts;

namespace Bazario.Core.Services.Inventory
{
    /// <summary>
    /// Implementation of inventory analytics and reporting
    /// Handles inventory reports, forecasting, and analytics
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class InventoryAnalyticsService : IInventoryAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryAnalyticsService> _logger;

        public InventoryAnalyticsService(
            IUnitOfWork unitOfWork,
            ILogger<InventoryAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InventoryReport> GenerateInventoryReportAsync(
            InventoryReportRequest reportRequest, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating inventory report for store {StoreId}", reportRequest.StoreId);

            var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(reportRequest.StoreId!.Value, includeDeleted: false, cancellationToken);
            var activeProducts = products.Where(p => !p.IsDeleted).ToList();

            var reportItems = activeProducts.Select(p => new InventoryReportItem
            {
                ProductId = p.ProductId,
                ProductName = p.Name,
                CurrentStock = p.StockQuantity,
                ReservedStock = 0,
                AvailableStock = p.StockQuantity,
                UnitPrice = p.Price,
                TotalValue = p.StockQuantity * p.Price,
                IsLowStock = p.StockQuantity <= 10,
                LastMovement = DateTime.UtcNow
            }).ToList();

            return new InventoryReport
            {
                ReportType = reportRequest.ReportType,
                GeneratedAt = DateTime.UtcNow,
                DateRange = reportRequest.DateRange,
                Items = reportItems,
                Summary = new InventoryReportSummary
                {
                    TotalProducts = activeProducts.Count,
                    TotalInventoryValue = reportItems.Sum(i => i.TotalValue),
                    InStockProducts = reportItems.Count(i => i.CurrentStock > 0),
                    LowStockProducts = reportItems.Count(i => i.IsLowStock),
                    OutOfStockProducts = reportItems.Count(i => i.CurrentStock == 0)
                }
            };
        }

        public async Task<List<InventoryTurnoverData>> GetInventoryTurnoverAsync(
            Guid? storeId = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calculating inventory turnover for store {StoreId}", storeId);

            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-6);
                var end = endDate ?? DateTime.UtcNow;

                // Get products for the store
                var products = storeId.HasValue
                    ? await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId.Value, includeDeleted: false, cancellationToken)
                    : await _unitOfWork.Products.GetAllProductsAsync(cancellationToken);

                var activeProducts = products.Where(p => !p.IsDeleted).ToList();
                var turnoverData = new List<InventoryTurnoverData>();

                foreach (var product in activeProducts)
                {
                    // Get sales data for this product in the date range
                    var totalSold = await GetProductSalesQuantityAsync(product.ProductId, start, end, cancellationToken);
                    var totalRevenue = await GetProductRevenueAsync(product.ProductId, start, end, cancellationToken);

                    // Calculate turnover metrics
                    var averageInventory = product.StockQuantity; // Simplified - in real app, calculate average over period
                    var costOfGoodsSold = totalSold * product.Price; // Simplified COGS
                    var turnoverRate = averageInventory > 0 ? costOfGoodsSold / averageInventory : 0;
                    var daysToSell = turnoverRate > 0 ? (int)(365 / turnoverRate) : 0;

                    turnoverData.Add(new InventoryTurnoverData
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name ?? string.Empty,
                        TurnoverRate = turnoverRate,
                        TotalSold = totalSold,
                        AverageStockLevel = averageInventory,
                        DaysToSell = daysToSell,
                        CalculatedAt = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Calculated turnover data for {Count} products", turnoverData.Count);
                return turnoverData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate inventory turnover for store {StoreId}", storeId);
                return new List<InventoryTurnoverData>();
            }
        }

        public async Task<StockValuationReport> GetStockValuationAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting stock valuation for store {StoreId}", storeId);

            var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);
            var activeProducts = products.Where(p => !p.IsDeleted).ToList();

            var valuations = activeProducts.Select(p => new ProductValuation
            {
                ProductId = p.ProductId,
                ProductName = p.Name ?? string.Empty,
                Quantity = p.StockQuantity,
                UnitPrice = p.Price,
                TotalValue = p.StockQuantity * p.Price
            }).ToList();

            var totalValue = valuations.Sum(v => v.TotalValue);
            
            foreach (var valuation in valuations)
            {
                valuation.PercentageOfTotal = totalValue > 0 ? (valuation.TotalValue / totalValue) * 100 : 0;
            }

            return new StockValuationReport
            {
                StoreId = storeId,
                TotalStockValue = totalValue,
                TotalProducts = activeProducts.Count,
                TotalQuantity = valuations.Sum(v => v.Quantity),
                AverageProductValue = activeProducts.Count > 0 ? totalValue / activeProducts.Count : 0,
                ProductValuations = valuations,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<InventoryPerformanceMetrics> GetInventoryPerformanceMetricsAsync(
            Guid storeId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calculating inventory performance metrics for store {StoreId}", storeId);

            try
            {
                // Get products for the store
                var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);
                var activeProducts = products.Where(p => !p.IsDeleted).ToList();

                // Calculate various performance metrics
                var totalInventoryValue = activeProducts.Sum(p => p.StockQuantity * p.Price);
                var totalProducts = activeProducts.Count;
                var inStockProducts = activeProducts.Count(p => p.StockQuantity > 0);
                var outOfStockProducts = activeProducts.Count(p => p.StockQuantity == 0);
                var lowStockProducts = activeProducts.Count(p => p.StockQuantity <= 10);

                // Calculate performance metrics based on available data
                var averageTurnoverRate = 0m; // Would calculate from actual sales data
                var stockoutOccurrences = outOfStockProducts;
                var stockoutRate = totalProducts > 0 ? (decimal)outOfStockProducts / totalProducts * 100 : 0;
                var averageInventoryHoldingCost = totalInventoryValue * 0.1m; // 10% holding cost assumption
                var daysOfInventoryOnHand = 30; // Placeholder
                var inventoryAccuracy = 0.95m; // 95% accuracy assumption
                var totalStockMovements = 0; // Would track from actual movements
                var lowStockAlerts = lowStockProducts;
                var overstockedProducts = activeProducts.Count(p => p.StockQuantity > 100); // Arbitrary threshold

                return new InventoryPerformanceMetrics
                {
                    StoreId = storeId,
                    StartDate = startDate,
                    EndDate = endDate,
                    CalculatedAt = DateTime.UtcNow,
                    AverageTurnoverRate = averageTurnoverRate,
                    StockoutOccurrences = stockoutOccurrences,
                    StockoutRate = stockoutRate,
                    TotalStockValue = totalInventoryValue,
                    AverageInventoryHoldingCost = averageInventoryHoldingCost,
                    DaysOfInventoryOnHand = daysOfInventoryOnHand,
                    InventoryAccuracy = inventoryAccuracy,
                    TotalStockMovements = totalStockMovements,
                    LowStockAlerts = lowStockAlerts,
                    OutOfStockProducts = outOfStockProducts,
                    OverstockedProducts = overstockedProducts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate inventory performance metrics for store {StoreId}", storeId);
                return new InventoryPerformanceMetrics
                {
                    StoreId = storeId,
                    StartDate = startDate,
                    EndDate = endDate,
                    CalculatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<List<StockForecast>> ForecastStockNeedsAsync(
            Guid storeId,
            int forecastDays = 30,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Forecasting stock needs for store {StoreId} for {Days} days", storeId, forecastDays);

            try
            {
                // Get products for the store
                var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);
                var activeProducts = products.Where(p => !p.IsDeleted).ToList();

                var forecasts = new List<StockForecast>();
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-90); // Look at last 90 days for trend analysis

                foreach (var product in activeProducts)
                {
                    // Get historical sales data
                    var totalSold = await GetProductSalesQuantityAsync(product.ProductId, startDate, endDate, cancellationToken);
                    var daysInPeriod = (endDate - startDate).Days;
                    var dailyAverageSales = daysInPeriod > 0 ? (double)totalSold / daysInPeriod : 0;

                    // Simple forecasting: daily average * forecast days
                    var forecastedDemand = (int)Math.Ceiling(dailyAverageSales * forecastDays);
                    
                    // Calculate recommended reorder quantity
                    var currentStock = product.StockQuantity;
                    var recommendedReorderQuantity = Math.Max(0, forecastedDemand - currentStock);

                    // Calculate confidence level based on sales consistency
                    var confidenceLevel = CalculateConfidenceLevel(totalSold, daysInPeriod);

                    forecasts.Add(new StockForecast
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name ?? string.Empty,
                        CurrentStock = currentStock,
                        ForecastedDemand = forecastedDemand,
                        RecommendedReorderQuantity = recommendedReorderQuantity,
                        ReorderDate = DateTime.UtcNow.AddDays(7), // Placeholder
                        LeadTimeDays = 7, // Placeholder
                        ConfidenceLevel = (decimal)confidenceLevel,
                        ForecastMethod = "Simple Moving Average",
                        CalculatedAt = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Generated {Count} stock forecasts for store {StoreId}", forecasts.Count, storeId);
                return forecasts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forecast stock needs for store {StoreId}", storeId);
                return new List<StockForecast>();
            }
        }

        private double CalculateConfidenceLevel(int totalSales, int daysInPeriod)
        {
            if (daysInPeriod <= 0 || totalSales == 0) return 0.1; // Low confidence for no data

            var dailyAverage = (double)totalSales / daysInPeriod;
            
            // Higher confidence for more consistent sales
            if (dailyAverage >= 1) return 0.9; // High confidence for daily sales
            if (dailyAverage >= 0.5) return 0.7; // Medium-high confidence
            if (dailyAverage >= 0.1) return 0.5; // Medium confidence
            return 0.3; // Low confidence for very low sales
        }

        public async Task<List<DeadStockItem>> GetDeadStockAnalysisAsync(
            Guid storeId,
            int daysSinceLastSale = 90,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing dead stock for store {StoreId}", storeId);

            try
            {
                // Get products for the store
                var products = await _unitOfWork.Products.GetProductsByStoreIdAsync(storeId, includeDeleted: false, cancellationToken);
                var activeProducts = products.Where(p => !p.IsDeleted).ToList();

                var deadStockItems = new List<DeadStockItem>();
                var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastSale);

                foreach (var product in activeProducts)
                {
                    // Get sales data for this product
                    var totalSold = await GetProductSalesQuantityAsync(product.ProductId, DateTime.MinValue, DateTime.UtcNow, cancellationToken);
                    var hasRecentSales = totalSold > 0;
                    
                    // If no sales or very low sales, consider it dead stock
                    if (!hasRecentSales || totalSold < 5) // Threshold for dead stock
                    {
                        var stockValue = product.StockQuantity * product.Price;
                        var daysSinceLastSaleValue = totalSold > 0 ? 
                            (DateTime.UtcNow - (product.CreatedAt ?? DateTime.UtcNow)).Days : 
                            (DateTime.UtcNow - (product.CreatedAt ?? DateTime.UtcNow)).Days;

                        deadStockItems.Add(new DeadStockItem
                        {
                            ProductId = product.ProductId,
                            ProductName = product.Name ?? string.Empty,
                            CurrentStock = product.StockQuantity,
                            StockValue = stockValue,
                            UnitPrice = product.Price,
                            DaysSinceLastSale = daysSinceLastSaleValue,
                            LastSaleDate = totalSold > 0 ? DateTime.UtcNow.AddDays(-daysSinceLastSaleValue) : null,
                            Recommendation = GetDeadStockRecommendation(stockValue, daysSinceLastSaleValue, product.StockQuantity),
                            AnalyzedAt = DateTime.UtcNow
                        });
                    }
                }

                // Sort by stock value (highest first) to prioritize high-value dead stock
                deadStockItems = deadStockItems.OrderByDescending(d => d.StockValue).ToList();

                _logger.LogInformation("Found {Count} dead stock items for store {StoreId}", deadStockItems.Count, storeId);
                return deadStockItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze dead stock for store {StoreId}", storeId);
                return new List<DeadStockItem>();
            }
        }

        private string GetDeadStockRecommendation(decimal totalValue, int daysSinceLastSale, int currentStock)
        {
            if (totalValue > 1000 && daysSinceLastSale > 180)
                return "High-value dead stock - consider liquidation sale or bundling";

            if (totalValue > 500 && daysSinceLastSale > 120)
                return "Medium-value dead stock - consider discount promotion";

            if (daysSinceLastSale > 90)
                return "Low-value dead stock - consider clearance sale";

            if (currentStock > 50)
                return "High inventory - consider reducing order quantity";

            return "Monitor closely - may need marketing push";
        }

        public async Task<Guid> GetStoreIdForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query the database to get the store ID for the product
                var productEntity = await _unitOfWork.Products.GetProductByIdAsync(productId, cancellationToken);
                if (productEntity != null)
                {
                    return productEntity.StoreId;
                }

                _logger.LogWarning("Product {ProductId} not found when getting store ID", productId);
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store ID for product {ProductId}", productId);
                return Guid.Empty;
            }
        }

        public async Task<int> GetProductSalesQuantityAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query order items to get sales quantity for the product
                var orders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                var ordersInRange = orders.Where(o => o.Date >= startDate && o.Date <= endDate).ToList();

                var totalQuantity = 0;
                foreach (var order in ordersInRange)
                {
                    // Get order items for this order and product
                    if (order.OrderItems != null)
                    {
                        var productItems = order.OrderItems.Where(oi => oi.ProductId == productId);
                        totalQuantity += productItems.Sum(oi => oi.Quantity);
                    }
                }

                return totalQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales quantity for product {ProductId}", productId);
                return 0;
            }
        }

        public async Task<decimal> GetProductRevenueAsync(Guid productId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query order items to get revenue for the product
                var orders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                var ordersInRange = orders.Where(o => o.Date >= startDate && o.Date <= endDate).ToList();

                var totalRevenue = 0m;
                foreach (var order in ordersInRange)
                {
                    // Get order items for this order and product
                    if (order.OrderItems != null)
                    {
                        var productItems = order.OrderItems.Where(oi => oi.ProductId == productId);
                        totalRevenue += productItems.Sum(oi => oi.Price * oi.Quantity);
                    }
                }

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue for product {ProductId}", productId);
                return 0;
            }
        }

    }
}
