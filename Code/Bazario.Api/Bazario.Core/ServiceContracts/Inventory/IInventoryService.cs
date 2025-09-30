using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.DTO;
using Bazario.Core.Models.Inventory;
using Bazario.Core.Models.Store;
using Bazario.Core.Models.Shared;

namespace Bazario.Core.ServiceContracts.Inventory
{
    /// <summary>
    /// Composite service contract for inventory management operations
    /// Combines all specialized inventory services following SOLID principles
    /// </summary>
    public interface IInventoryService : 
        IInventoryManagementService,
        IInventoryQueryService,
        IInventoryValidationService,
        IInventoryAnalyticsService,
        IInventoryAlertService
    {
        // This interface combines all specialized services
        // No additional methods needed - inherits from specialized interfaces
    }
}
