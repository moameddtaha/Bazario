using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Enums;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories
{
    public class StoreShippingRepository : IStoreShippingRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<StoreShippingRate> _dbSet;

        public StoreShippingRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<StoreShippingRate>();
        }

        public async Task<StoreShippingRate> AddStoreShippingRateAsync(StoreShippingRate storeShippingRate, CancellationToken cancellationToken = default)
        {
            _dbSet.Add(storeShippingRate);
            await _context.SaveChangesAsync(cancellationToken);
            return storeShippingRate;
        }

        public async Task<StoreShippingRate> UpdateStoreShippingRateAsync(StoreShippingRate storeShippingRate, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(storeShippingRate);
            await _context.SaveChangesAsync(cancellationToken);
            return storeShippingRate;
        }

        public async Task<bool> DeleteStoreShippingRateAsync(Guid storeShippingRateId, CancellationToken cancellationToken = default)
        {
            var storeShippingRate = await _dbSet.FindAsync(storeShippingRateId);
            if (storeShippingRate == null) return false;

            _dbSet.Remove(storeShippingRate);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<StoreShippingRate?> GetStoreShippingRateByIdAsync(Guid storeShippingRateId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.StoreShippingRateId == storeShippingRateId, cancellationToken);
        }

        public async Task<List<StoreShippingRate>> GetActiveShippingRatesByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .Where(s => s.StoreId == storeId && s.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<StoreShippingRate?> GetShippingRateByZoneAsync(Guid storeId, ShippingZone zone, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.StoreId == storeId && s.ShippingZone == zone, cancellationToken);
        }

        public async Task<StoreShippingRate> CreateOrUpdateShippingRateAsync(Guid storeId, ShippingZone zone, decimal shippingCost, decimal freeShippingThreshold, Guid updatedBy, CancellationToken cancellationToken = default)
        {
            var existingRate = await GetShippingRateByZoneAsync(storeId, zone, cancellationToken);
            
            if (existingRate != null)
            {
                existingRate.ShippingCost = shippingCost;
                existingRate.FreeShippingThreshold = freeShippingThreshold;
                existingRate.UpdatedAt = DateTime.UtcNow;
                existingRate.UpdatedBy = updatedBy;
                
                return await UpdateStoreShippingRateAsync(existingRate, cancellationToken);
            }
            else
            {
                var newRate = new StoreShippingRate
                {
                    StoreId = storeId,
                    ShippingZone = zone,
                    ShippingCost = shippingCost,
                    FreeShippingThreshold = freeShippingThreshold,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = updatedBy
                };
                
                return await AddStoreShippingRateAsync(newRate, cancellationToken);
            }
        }

        public async Task<List<StoreShippingRate>> GetShippingRatesByStoreIdsAsync(List<Guid> storeIds, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .Where(s => storeIds.Contains(s.StoreId))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StoreShippingRate>> GetShippingRatesByZoneAsync(ShippingZone zone, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .Where(s => s.ShippingZone == zone)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasShippingRatesAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(s => s.StoreId == storeId, cancellationToken);
        }

        public async Task<int> GetShippingRatesCountByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(s => s.StoreId == storeId, cancellationToken);
        }

        public IQueryable<StoreShippingRate> GetStoreShippingRatesQueryable()
        {
            return _dbSet.Include(s => s.Store).AsQueryable();
        }

        public async Task<int> GetStoreShippingRatesCountAsync(IQueryable<StoreShippingRate> query, CancellationToken cancellationToken = default)
        {
            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<StoreShippingRate>> GetStoreShippingRatesPagedAsync(IQueryable<StoreShippingRate> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StoreShippingRate>> GetShippingRatesByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .Where(s => s.StoreId == storeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<StoreShippingRate?> GetBestShippingRateAsync(Guid storeId, ShippingZone zone, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Store)
                .Where(s => s.StoreId == storeId && s.ShippingZone == zone && s.IsActive)
                .OrderBy(s => s.ShippingCost)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
