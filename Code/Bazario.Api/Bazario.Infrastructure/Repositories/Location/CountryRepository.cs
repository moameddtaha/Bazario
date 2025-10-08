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
    /// Repository implementation for Country entity operations
    /// </summary>
    public class CountryRepository : ICountryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CountryRepository> _logger;

        public CountryRepository(ApplicationDbContext context, ILogger<CountryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Country?> GetByIdAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting country by ID: {CountryId}", countryId);

            return await _context.Countries
                .Include(c => c.Governorates)
                .FirstOrDefaultAsync(c => c.CountryId == countryId, cancellationToken);
        }

        public async Task<Country?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting country by code: {Code}", code);

            return await _context.Countries
                .Include(c => c.Governorates)
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper(), cancellationToken);
        }

        public async Task<List<Country>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting all countries");

            return await _context.Countries
                .Include(c => c.Governorates)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Country>> GetActiveCountriesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting active countries");

            return await _context.Countries
                .Include(c => c.Governorates.Where(g => g.IsActive))
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Country> AddAsync(Country country, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adding new country: {CountryName} ({CountryCode})", country.Name, country.Code);

            country.CreatedAt = DateTime.UtcNow;
            country.UpdatedAt = DateTime.UtcNow;

            _context.Countries.Add(country);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added country: {CountryId}", country.CountryId);
            return country;
        }

        public async Task<Country> UpdateAsync(Country country, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating country: {CountryId}", country.CountryId);

            var existingCountry = await _context.Countries.FindAsync(new object[] { country.CountryId }, cancellationToken);
            if (existingCountry == null)
            {
                throw new InvalidOperationException($"Country with ID {country.CountryId} not found");
            }

            // Only update specific allowed fields
            existingCountry.Name = country.Name;
            existingCountry.NameArabic = country.NameArabic;
            existingCountry.IsActive = country.IsActive;
            existingCountry.SupportsPostalCodes = country.SupportsPostalCodes;
            existingCountry.UpdatedAt = DateTime.UtcNow;

            // Do NOT update: CountryId, Code, CreatedAt

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated country: {CountryId}", country.CountryId);
            return existingCountry;
        }

        public async Task<bool> DeactivateAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating country: {CountryId}", countryId);

            var country = await _context.Countries.FindAsync(new object[] { countryId }, cancellationToken);
            if (country == null)
            {
                _logger.LogWarning("Country not found: {CountryId}", countryId);
                return false;
            }

            country.IsActive = false;
            country.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deactivated country: {CountryId}", countryId);
            return true;
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if country code exists: {Code}", code);

            return await _context.Countries
                .AnyAsync(c => c.Code.ToUpper() == code.ToUpper(), cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if country name exists: {Name}", name);

            return await _context.Countries
                .AnyAsync(c => c.Name.ToUpper() == name.ToUpper(), cancellationToken);
        }
    }
}
