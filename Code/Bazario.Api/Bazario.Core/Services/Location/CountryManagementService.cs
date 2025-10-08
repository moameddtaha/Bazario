using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.Country;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts.Location;

namespace Bazario.Core.Services.Location
{
    public class CountryManagementService : ICountryManagementService
    {
        private readonly ICountryRepository _countryRepository;
        private readonly ILogger<CountryManagementService> _logger;

        public CountryManagementService(
            ICountryRepository countryRepository,
            ILogger<CountryManagementService> logger)
        {
            _countryRepository = countryRepository ?? throw new ArgumentNullException(nameof(countryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CountryResponse> CreateCountryAsync(CountryAddRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new country: {CountryName} ({Code})", request.Name, request.Code);

            // Validate uniqueness
            if (await _countryRepository.ExistsByCodeAsync(request.Code, cancellationToken))
            {
                throw new InvalidOperationException($"A country with code '{request.Code}' already exists");
            }

            if (await _countryRepository.ExistsByNameAsync(request.Name, cancellationToken))
            {
                throw new InvalidOperationException($"A country with name '{request.Name}' already exists");
            }

            // Create entity
            var country = new Country
            {
                Name = request.Name,
                Code = request.Code.ToUpperInvariant(),
                NameArabic = request.NameArabic,
                SupportsPostalCodes = request.SupportsPostalCodes,
                IsActive = true
            };

            var createdCountry = await _countryRepository.AddAsync(country, cancellationToken);

            _logger.LogInformation("Successfully created country: {CountryId}", createdCountry.CountryId);

            return new CountryResponse
            {
                CountryId = createdCountry.CountryId,
                Name = createdCountry.Name,
                Code = createdCountry.Code,
                NameArabic = createdCountry.NameArabic,
                IsActive = createdCountry.IsActive,
                SupportsPostalCodes = createdCountry.SupportsPostalCodes,
                GovernorateCount = 0,
                CreatedAt = createdCountry.CreatedAt,
                UpdatedAt = createdCountry.UpdatedAt
            };
        }

        public async Task<CountryResponse> UpdateCountryAsync(CountryUpdateRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating country: {CountryId}", request.CountryId);

            var existingCountry = await _countryRepository.GetByIdAsync(request.CountryId, cancellationToken);
            if (existingCountry == null)
            {
                throw new InvalidOperationException($"Country with ID {request.CountryId} not found");
            }

            // Check name uniqueness (excluding current country)
            var countries = await _countryRepository.GetAllAsync(cancellationToken);
            if (countries.Any(c => c.Name == request.Name && c.CountryId != request.CountryId))
            {
                throw new InvalidOperationException($"A country with name '{request.Name}' already exists");
            }

            // Update entity
            existingCountry.Name = request.Name;
            existingCountry.NameArabic = request.NameArabic;
            existingCountry.IsActive = request.IsActive;
            existingCountry.SupportsPostalCodes = request.SupportsPostalCodes;

            var updatedCountry = await _countryRepository.UpdateAsync(existingCountry, cancellationToken);

            _logger.LogInformation("Successfully updated country: {CountryId}", updatedCountry.CountryId);

            return new CountryResponse
            {
                CountryId = updatedCountry.CountryId,
                Name = updatedCountry.Name,
                Code = updatedCountry.Code,
                NameArabic = updatedCountry.NameArabic,
                IsActive = updatedCountry.IsActive,
                SupportsPostalCodes = updatedCountry.SupportsPostalCodes,
                GovernorateCount = updatedCountry.Governorates?.Count ?? 0,
                CreatedAt = updatedCountry.CreatedAt,
                UpdatedAt = updatedCountry.UpdatedAt
            };
        }

        public async Task<CountryResponse?> GetCountryByIdAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            var country = await _countryRepository.GetByIdAsync(countryId, cancellationToken);
            if (country == null)
                return null;

            return new CountryResponse
            {
                CountryId = country.CountryId,
                Name = country.Name,
                Code = country.Code,
                NameArabic = country.NameArabic,
                IsActive = country.IsActive,
                SupportsPostalCodes = country.SupportsPostalCodes,
                GovernorateCount = country.Governorates?.Count ?? 0,
                CreatedAt = country.CreatedAt,
                UpdatedAt = country.UpdatedAt
            };
        }

        public async Task<CountryResponse?> GetCountryByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var country = await _countryRepository.GetByCodeAsync(code, cancellationToken);
            if (country == null)
                return null;

            return new CountryResponse
            {
                CountryId = country.CountryId,
                Name = country.Name,
                Code = country.Code,
                NameArabic = country.NameArabic,
                IsActive = country.IsActive,
                SupportsPostalCodes = country.SupportsPostalCodes,
                GovernorateCount = country.Governorates?.Count ?? 0,
                CreatedAt = country.CreatedAt,
                UpdatedAt = country.UpdatedAt
            };
        }

        public async Task<List<CountryResponse>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
        {
            var countries = await _countryRepository.GetAllAsync(cancellationToken);

            return countries.Select(c => new CountryResponse
            {
                CountryId = c.CountryId,
                Name = c.Name,
                Code = c.Code,
                NameArabic = c.NameArabic,
                IsActive = c.IsActive,
                SupportsPostalCodes = c.SupportsPostalCodes,
                GovernorateCount = c.Governorates?.Count ?? 0,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<CountryResponse>> GetActiveCountriesAsync(CancellationToken cancellationToken = default)
        {
            var countries = await _countryRepository.GetActiveCountriesAsync(cancellationToken);

            return countries.Select(c => new CountryResponse
            {
                CountryId = c.CountryId,
                Name = c.Name,
                Code = c.Code,
                NameArabic = c.NameArabic,
                IsActive = c.IsActive,
                SupportsPostalCodes = c.SupportsPostalCodes,
                GovernorateCount = c.Governorates?.Count ?? 0,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<bool> DeactivateCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating country: {CountryId}", countryId);

            var result = await _countryRepository.DeactivateAsync(countryId, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully deactivated country: {CountryId}", countryId);
            }
            else
            {
                _logger.LogWarning("Failed to deactivate country: {CountryId}", countryId);
            }

            return result;
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _countryRepository.ExistsByCodeAsync(code, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _countryRepository.ExistsByNameAsync(name, cancellationToken);
        }
    }
}
