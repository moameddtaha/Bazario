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
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.ServiceContracts.Authorization;

namespace Bazario.Core.Services.Location
{
    /// <summary>
    /// Service for managing country entities.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// Requires admin privileges for write operations (Create, Update, Deactivate).
    /// </summary>
    public class CountryManagementService : ICountryManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminAuthorizationService _adminAuthService;
        private readonly ILogger<CountryManagementService> _logger;

        public CountryManagementService(
            IUnitOfWork unitOfWork,
            IAdminAuthorizationService adminAuthHelper,
            ILogger<CountryManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _adminAuthService = adminAuthHelper ?? throw new ArgumentNullException(nameof(adminAuthHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CountryResponse> CreateCountryAsync(CountryAddRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Country name is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new ArgumentException("Country code is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} creating new country: {CountryName} ({Code})", userId, request.Name, request.Code);

            try
            {
                // Validate uniqueness
                if (await _unitOfWork.Countries.ExistsByCodeAsync(request.Code, cancellationToken))
                {
                    throw new InvalidOperationException($"A country with code '{request.Code}' already exists");
                }

                if (await _unitOfWork.Countries.ExistsByNameAsync(request.Name, cancellationToken))
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

                var createdCountry = await _unitOfWork.Countries.AddAsync(country, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            catch (ArgumentException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create country: {CountryName} ({Code})", request.Name, request.Code);
                throw new InvalidOperationException($"Failed to create country '{request.Name}': {ex.Message}", ex);
            }
        }

        public async Task<CountryResponse> UpdateCountryAsync(CountryUpdateRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            if (request.CountryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Country name is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} updating country: {CountryId}", userId, request.CountryId);

            try
            {
                var existingCountry = await _unitOfWork.Countries.GetByIdAsync(request.CountryId, cancellationToken);
                if (existingCountry == null)
                {
                    throw new InvalidOperationException($"Country with ID {request.CountryId} not found");
                }

                // Check name uniqueness (excluding current country)
                var countries = await _unitOfWork.Countries.GetAllAsync(cancellationToken);
                if (countries.Any(c => c.Name == request.Name && c.CountryId != request.CountryId))
                {
                    throw new InvalidOperationException($"A country with name '{request.Name}' already exists");
                }

                // Update entity
                existingCountry.Name = request.Name;
                existingCountry.NameArabic = request.NameArabic;
                existingCountry.IsActive = request.IsActive;
                existingCountry.SupportsPostalCodes = request.SupportsPostalCodes;

                var updatedCountry = await _unitOfWork.Countries.UpdateAsync(existingCountry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            catch (ArgumentException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update country: {CountryId}", request.CountryId);
                throw new InvalidOperationException($"Failed to update country {request.CountryId}: {ex.Message}", ex);
            }
        }

        public async Task<CountryResponse?> GetCountryByIdAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            try
            {
                var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get country: {CountryId}", countryId);
                throw new InvalidOperationException($"Failed to get country {countryId}: {ex.Message}", ex);
            }
        }

        public async Task<CountryResponse?> GetCountryByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Country code is required", nameof(code));
            }

            try
            {
                var country = await _unitOfWork.Countries.GetByCodeAsync(code, cancellationToken);
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get country by code: {Code}", code);
                throw new InvalidOperationException($"Failed to get country by code '{code}': {ex.Message}", ex);
            }
        }

        public async Task<List<CountryResponse>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var countries = await _unitOfWork.Countries.GetAllAsync(cancellationToken);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all countries");
                throw new InvalidOperationException($"Failed to get all countries: {ex.Message}", ex);
            }
        }

        public async Task<List<CountryResponse>> GetActiveCountriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var countries = await _unitOfWork.Countries.GetActiveCountriesAsync(cancellationToken);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active countries");
                throw new InvalidOperationException($"Failed to get active countries: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeactivateCountryAsync(Guid countryId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} deactivating country: {CountryId}", userId, countryId);

            try
            {
                // Check if country exists first
                var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);
                if (country == null)
                {
                    throw new InvalidOperationException($"Country with ID {countryId} not found");
                }

                var result = await _unitOfWork.Countries.DeactivateAsync(countryId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            catch (ArgumentException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate country: {CountryId}", countryId);
                throw new InvalidOperationException($"Failed to deactivate country {countryId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Country code is required", nameof(code));
            }

            try
            {
                return await _unitOfWork.Countries.ExistsByCodeAsync(code, cancellationToken);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if country exists by code: {Code}", code);
                throw new InvalidOperationException($"Failed to check if country code '{code}' exists: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Country name is required", nameof(name));
            }

            try
            {
                return await _unitOfWork.Countries.ExistsByNameAsync(name, cancellationToken);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if country exists by name: {Name}", name);
                throw new InvalidOperationException($"Failed to check if country name '{name}' exists: {ex.Message}", ex);
            }
        }
    }
}
