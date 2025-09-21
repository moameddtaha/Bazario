using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Product;
using Bazario.Core.Models.Order;
using Bazario.Core.Models.Product;

namespace Bazario.Core.ServiceContracts.Product
{
    /// <summary>
    /// Composite service contract for product operations
    /// Combines all specialized product services into a single interface
    /// Follows Interface Segregation Principle by inheriting from specialized interfaces
    /// </summary>
    public interface IProductService : IProductManagementService, IProductQueryService, IProductInventoryService, IProductAnalyticsService, IProductValidationService
    {
        // This interface inherits all methods from the specialized interfaces:
        // - IProductManagementService: CreateProductAsync, UpdateProductAsync, DeleteProductAsync
        // - IProductQueryService: GetProductByIdAsync, GetProductsByStoreIdAsync, SearchProductsAsync, GetLowStockProductsAsync
        // - IProductInventoryService: UpdateStockAsync, ReserveStockAsync, ReleaseStockAsync
        // - IProductAnalyticsService: GetProductAnalyticsAsync
        // - IProductValidationService: ValidateForOrderAsync
    }

}
