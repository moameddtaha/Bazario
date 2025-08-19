using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.DTO;

namespace Bazario.Core.Extensions
{
    public static class StoreExtensions
    {
        public static StoreResponse ToStoreResponse(this Store store)
        {
            return new StoreResponse
            {
                StoreId = store.StoreId,
                SellerId = store.SellerId,
                Name = store.Name,
                Description = store.Description,
                Category = store.Category,
                Logo = store.Logo,
                CreatedAt = store.CreatedAt
            };
        }
    }
}
