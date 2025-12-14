using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoreEntity = Bazario.Core.Domain.Entities.Store.Store;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Store;

namespace Bazario.Core.Extensions.Store
{
    public static class StoreExtensions
    {
        public static StoreResponse ToStoreResponse(this StoreEntity store)
        {
            return new StoreResponse
            {
                StoreId = store.StoreId,
                SellerId = store.SellerId,
                Name = store.Name,
                Description = store.Description,
                Category = store.Category,
                Logo = store.Logo,
                CreatedAt = store.CreatedAt,
                IsActive = store.IsActive,
                // Soft deletion properties
                IsDeleted = store.IsDeleted,
                DeletedAt = store.DeletedAt,
                DeletedBy = store.DeletedBy,
                DeletedReason = store.DeletedReason,
                // Concurrency control
                RowVersion = store.RowVersion
            };
        }
    }
}
