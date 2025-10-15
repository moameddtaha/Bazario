using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Bazario.Infrastructure.Repositories.Location
{
    public class StoreGovernorateSupportRepository : IStoreGovernorateSupportRepository
    {
        private readonly ApplicationDbContext _context;

        public StoreGovernorateSupportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StoreGovernorateSupport>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _context.StoreGovernorateSupports
                .Include(s => s.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(s => s.StoreId == storeId)
                .OrderBy(s => s.Governorate.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StoreGovernorateSupport>> GetSupportedGovernorates(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _context.StoreGovernorateSupports
                .Include(s => s.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(s => s.StoreId == storeId && s.IsSupported)
                .OrderBy(s => s.Governorate.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StoreGovernorateSupport>> GetExcludedGovernorates(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _context.StoreGovernorateSupports
                .Include(s => s.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(s => s.StoreId == storeId && !s.IsSupported)
                .OrderBy(s => s.Governorate.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsGovernorateSupportedAsync(Guid storeId, Guid governorateId, CancellationToken cancellationToken = default)
        {
            var record = await _context.StoreGovernorateSupports
                .FirstOrDefaultAsync(s => s.StoreId == storeId && s.GovernorateId == governorateId, cancellationToken);

            return record?.IsSupported ?? false;
        }

        public async Task<StoreGovernorateSupport?> GetByStoreAndGovernorateAsync(Guid storeId, Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.StoreGovernorateSupports
                .Include(s => s.Governorate)
                    .ThenInclude(g => g.Country)
                .FirstOrDefaultAsync(s => s.StoreId == storeId && s.GovernorateId == governorateId, cancellationToken);
        }

        public async Task<StoreGovernorateSupport> AddAsync(StoreGovernorateSupport support, CancellationToken cancellationToken = default)
        {
            support.Id = Guid.NewGuid();
            support.CreatedAt = DateTime.UtcNow;
            support.UpdatedAt = DateTime.UtcNow;

            await _context.StoreGovernorateSupports.AddAsync(support, cancellationToken);

            return support;
        }

        public async Task AddRangeAsync(List<StoreGovernorateSupport> supports, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var support in supports)
            {
                support.Id = Guid.NewGuid();
                support.CreatedAt = now;
                support.UpdatedAt = now;
            }

            await _context.StoreGovernorateSupports.AddRangeAsync(supports, cancellationToken);
        }

        public async Task<StoreGovernorateSupport> UpdateAsync(StoreGovernorateSupport support, CancellationToken cancellationToken = default)
        {
            var existingSupport = await _context.StoreGovernorateSupports
                .FindAsync(new object[] { support.Id }, cancellationToken);

            if (existingSupport == null)
            {
                throw new InvalidOperationException($"StoreGovernorateSupport with ID {support.Id} not found");
            }

            // Only update allowed fields (safe update pattern)
            existingSupport.IsSupported = support.IsSupported;
            existingSupport.UpdatedAt = DateTime.UtcNow;

            // Do NOT update: Id, StoreId, GovernorateId, CreatedAt

            return existingSupport;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var support = await _context.StoreGovernorateSupports.FindAsync(new object[] { id }, cancellationToken);

            if (support != null)
            {
                _context.StoreGovernorateSupports.Remove(support);
            }
        }

        public async Task DeleteByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            var supports = await _context.StoreGovernorateSupports
                .Where(s => s.StoreId == storeId)
                .ToListAsync(cancellationToken);

            if (supports.Any())
            {
                _context.StoreGovernorateSupports.RemoveRange(supports);
            }
        }

        public async Task ReplaceStoreGovernorates(Guid storeId, List<StoreGovernorateSupport> newSupports, CancellationToken cancellationToken = default)
        {
            // Delete existing records
            await DeleteByStoreIdAsync(storeId, cancellationToken);

            // Add new records
            if (newSupports.Any())
            {
                await AddRangeAsync(newSupports, cancellationToken);
            }
        }
    }
}
