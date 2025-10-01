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
    public class DiscountRepository : IDiscountRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Discount> _dbSet;

        public DiscountRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<Discount>();
        }

        public async Task<Discount> AddDiscountAsync(Discount discount, CancellationToken cancellationToken = default)
        {
            _dbSet.Add(discount);
            await _context.SaveChangesAsync(cancellationToken);
            return discount;
        }

        public async Task<Discount> UpdateDiscountAsync(Discount discount, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(discount);
            await _context.SaveChangesAsync(cancellationToken);
            return discount;
        }

        public async Task<bool> SoftDeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            var discount = await _dbSet.FindAsync(discountId);
            if (discount == null) return false;

            discount.IsActive = false;
            discount.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<Discount?> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .FirstOrDefaultAsync(d => d.DiscountId == discountId, cancellationToken);
        }

        public async Task<Discount?> GetDiscountByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .FirstOrDefaultAsync(d => d.Code.ToLower() == code.ToLower(), cancellationToken);
        }

        public async Task<List<Discount>> GetDiscountsByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .Where(d => d.ApplicableStoreId == storeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Discount>> GetGlobalDiscountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .Where(d => d.ApplicableStoreId == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<(bool IsValid, Discount? Discount, string? ErrorMessage)> ValidateDiscountAsync(
            string code, 
            decimal orderSubtotal, 
            List<Guid> storeIds, 
            CancellationToken cancellationToken = default)
        {
            var discount = await GetDiscountByCodeAsync(code, cancellationToken);
            
            if (discount == null)
            {
                return (false, null, "Discount code not found");
            }

            if (!discount.IsActive)
            {
                return (false, null, "Discount code is not active");
            }

            if (discount.IsUsed)
            {
                return (false, null, "Discount code has already been used");
            }

            var now = DateTime.UtcNow;
            if (now < discount.ValidFrom)
            {
                return (false, null, "Discount code is not yet valid");
            }

            if (now > discount.ValidTo)
            {
                return (false, null, "Discount code has expired");
            }

            if (orderSubtotal < discount.MinimumOrderAmount)
            {
                return (false, null, $"Minimum order amount of {discount.MinimumOrderAmount:C} required");
            }

            // Check if discount applies to any of the stores in the order
            if (discount.ApplicableStoreId.HasValue && !storeIds.Contains(discount.ApplicableStoreId.Value))
            {
                return (false, null, "Discount code is not valid for this store");
            }

            return (true, discount, null);
        }

        public async Task<bool> MarkDiscountAsUsedAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            var discount = await _dbSet.FindAsync(discountId);
            if (discount == null) return false;

            discount.IsUsed = true;
            discount.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<Discount>> GetValidDiscountsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .Where(d => d.ValidFrom <= toDate && d.ValidTo >= fromDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Discount>> GetDiscountsByTypeAsync(DiscountType type, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Store)
                .Where(d => d.Type == type)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<Discount> GetDiscountsQueryable()
        {
            return _dbSet.Include(d => d.Store).AsQueryable();
        }

        public async Task<int> GetDiscountsCountAsync(IQueryable<Discount> query, CancellationToken cancellationToken = default)
        {
            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<Discount>> GetDiscountsPagedAsync(IQueryable<Discount> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Discount>> GetExpiringDiscountsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
        {
            var expiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry);
            return await _dbSet
                .Include(d => d.Store)
                .Where(d => d.ValidTo <= expiryDate && d.ValidTo > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public async Task<(int TotalCreated, int TotalUsed, int TotalActive)> GetDiscountUsageStatsAsync(CancellationToken cancellationToken = default)
        {
            var totalCreated = await _dbSet.CountAsync(cancellationToken);
            var totalUsed = await _dbSet.CountAsync(d => d.IsUsed, cancellationToken);
            var totalActive = await _dbSet.CountAsync(d => d.IsActive, cancellationToken);

            return (totalCreated, totalUsed, totalActive);
        }

        public async Task<List<Discount>> GetDiscountsByCodesAsync(List<string> codes, CancellationToken cancellationToken = default)
        {
            try
            {
                if (codes == null || !codes.Any())
                {
                    return new List<Discount>();
                }

                var discounts = await _dbSet
                    .Include(d => d.Store)
                    .Where(d => codes.Contains(d.Code))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                return discounts;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get discounts by codes: {ex.Message}", ex);
            }
        }
    }
}
