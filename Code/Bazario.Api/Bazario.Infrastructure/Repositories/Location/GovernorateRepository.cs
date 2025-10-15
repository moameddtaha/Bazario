using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Location
{
    /// <summary>
    /// Repository implementation for Governorate entity operations
    /// </summary>
    public class GovernorateRepository : IGovernorateRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GovernorateRepository> _logger;

        public GovernorateRepository(ApplicationDbContext context, ILogger<GovernorateRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Governorate?> GetByIdAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting governorate by ID: {GovernorateId}", governorateId);

            return await _context.Governorates
                .Include(g => g.Country)
                .FirstOrDefaultAsync(g => g.GovernorateId == governorateId, cancellationToken);
        }

        public async Task<List<Governorate>> GetByCountryIdAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting governorates for country: {CountryId}", countryId);

            return await _context.Governorates
                .Include(g => g.Country)
                .Where(g => g.CountryId == countryId)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Governorate>> GetActiveByCountryIdAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting active governorates for country: {CountryId}", countryId);

            return await _context.Governorates
                .Include(g => g.Country)
                .Where(g => g.CountryId == countryId && g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Governorate>> GetByIdsAsync(List<Guid> governorateIds, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting governorates by IDs: {Count} items", governorateIds.Count);

            return await _context.Governorates
                .Include(g => g.Country)
                .Where(g => governorateIds.Contains(g.GovernorateId))
                .ToListAsync(cancellationToken);
        }

        public async Task<Governorate?> GetByNameAndCountryAsync(string name, Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting governorate by name: {Name} in country: {CountryId}", name, countryId);

            return await _context.Governorates
                .Include(g => g.Country)
                .FirstOrDefaultAsync(g => g.Name.ToUpper() == name.ToUpper() && g.CountryId == countryId, cancellationToken);
        }

        public async Task<List<Governorate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting all governorates");

            return await _context.Governorates
                .Include(g => g.Country)
                .OrderBy(g => g.Country.Name)
                .ThenBy(g => g.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Governorate>> GetSameDayDeliveryGovernoratesAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting same-day delivery governorates for country: {CountryId}", countryId);

            return await _context.Governorates
                .Include(g => g.Country)
                .Where(g => g.CountryId == countryId && g.IsActive && g.SupportsSameDayDelivery)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<Governorate> AddAsync(Governorate governorate, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adding new governorate: {GovernmentName} in country: {CountryId}", governorate.Name, governorate.CountryId);

            governorate.CreatedAt = DateTime.UtcNow;
            governorate.UpdatedAt = DateTime.UtcNow;

            _context.Governorates.Add(governorate);

            _logger.LogInformation("Successfully added governorate: {GovernorateId}", governorate.GovernorateId);
            return Task.FromResult(governorate);
        }

        public async Task<Governorate> UpdateAsync(Governorate governorate, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating governorate: {GovernorateId}", governorate.GovernorateId);

            var existingGovernorate = await _context.Governorates.FindAsync(new object[] { governorate.GovernorateId }, cancellationToken);
            if (existingGovernorate == null)
            {
                throw new InvalidOperationException($"Governorate with ID {governorate.GovernorateId} not found");
            }

            // Only update specific allowed fields
            existingGovernorate.Name = governorate.Name;
            existingGovernorate.NameArabic = governorate.NameArabic;
            existingGovernorate.Code = governorate.Code;
            existingGovernorate.IsActive = governorate.IsActive;
            existingGovernorate.SupportsSameDayDelivery = governorate.SupportsSameDayDelivery;
            existingGovernorate.UpdatedAt = DateTime.UtcNow;

            // Do NOT update: GovernorateId, CountryId, CreatedAt


            _logger.LogInformation("Successfully updated governorate: {GovernorateId}", governorate.GovernorateId);
            return existingGovernorate;
        }

        public async Task<bool> DeactivateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating governorate: {GovernorateId}", governorateId);

            var governorate = await _context.Governorates.FindAsync(new object[] { governorateId }, cancellationToken);
            if (governorate == null)
            {
                _logger.LogWarning("Governorate not found: {GovernorateId}", governorateId);
                return false;
            }

            governorate.IsActive = false;
            governorate.UpdatedAt = DateTime.UtcNow;


            _logger.LogInformation("Successfully deactivated governorate: {GovernorateId}", governorateId);
            return true;
        }

        public async Task<bool> ExistsByNameInCountryAsync(string name, Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if governorate name exists: {Name} in country: {CountryId}", name, countryId);

            return await _context.Governorates
                .AnyAsync(g => g.Name.ToUpper() == name.ToUpper() && g.CountryId == countryId, cancellationToken);
        }
    }
}
