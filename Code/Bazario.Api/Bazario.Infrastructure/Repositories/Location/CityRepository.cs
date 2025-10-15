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
    public class CityRepository : ICityRepository
    {
        private readonly ApplicationDbContext _context;

        public CityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .FirstOrDefaultAsync(c => c.CityId == cityId, cancellationToken);
        }

        public async Task<City?> GetByNameAndGovernorateAsync(string name, Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .FirstOrDefaultAsync(c => c.Name.ToUpper() == name.ToUpper() && c.GovernorateId == governorateId, cancellationToken);
        }

        public async Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .OrderBy(c => c.Governorate.Name)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<City>> GetByGovernorateIdAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(c => c.GovernorateId == governorateId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<City>> GetActiveByGovernorateIdAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(c => c.GovernorateId == governorateId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<City>> GetSameDayDeliveryCitiesAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(c => c.GovernorateId == governorateId && c.IsActive && c.SupportsSameDayDelivery)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<City>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .Include(c => c.Governorate)
                    .ThenInclude(g => g.Country)
                .Where(c => c.IsActive &&
                           (c.Name.Contains(searchTerm) ||
                            c.NameArabic != null && c.NameArabic.Contains(searchTerm)))
                .OrderBy(c => c.Governorate.Name)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<City> AddAsync(City city, CancellationToken cancellationToken = default)
        {
            city.CityId = Guid.NewGuid();
            city.CreatedAt = DateTime.UtcNow;
            city.UpdatedAt = DateTime.UtcNow;

            await _context.Cities.AddAsync(city, cancellationToken);

            return city;
        }

        public async Task<City> UpdateAsync(City city, CancellationToken cancellationToken = default)
        {
            var existingCity = await _context.Cities.FindAsync(new object[] { city.CityId }, cancellationToken);

            if (existingCity == null)
            {
                throw new InvalidOperationException($"City with ID {city.CityId} not found");
            }

            // Safe update pattern - only update specific allowed fields
            existingCity.Name = city.Name;
            existingCity.NameArabic = city.NameArabic;
            existingCity.Code = city.Code;
            existingCity.IsActive = city.IsActive;
            existingCity.SupportsSameDayDelivery = city.SupportsSameDayDelivery;
            existingCity.UpdatedAt = DateTime.UtcNow;

            // Do NOT update: CityId, GovernorateId, CreatedAt

            return existingCity;
        }

        public async Task<bool> DeactivateAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            var city = await _context.Cities.FindAsync(new object[] { cityId }, cancellationToken);

            if (city == null)
                return false;

            city.IsActive = false;
            city.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _context.Cities
                .AnyAsync(c => c.Name.ToUpper() == name.ToUpper() && c.GovernorateId == governorateId, cancellationToken);
        }
    }
}
