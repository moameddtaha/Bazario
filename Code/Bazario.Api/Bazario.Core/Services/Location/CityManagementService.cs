using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.City;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.ServiceContracts.Authorization;

namespace Bazario.Core.Services.Location
{
    /// <summary>
    /// Service for managing city entities.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// Requires admin privileges for write operations (Create, Update, Deactivate).
    /// </summary>
    public class CityManagementService : ICityManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminAuthorizationService _adminAuthService;
        private readonly ILogger<CityManagementService> _logger;

        public CityManagementService(
            IUnitOfWork unitOfWork,
            IAdminAuthorizationService adminAuthHelper,
            ILogger<CityManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _adminAuthService = adminAuthHelper ?? throw new ArgumentNullException(nameof(adminAuthHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CityResponse> CreateCityAsync(CityAddRequest request, Guid userId, CancellationToken cancellationToken = default)
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

            if (request.GovernorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("City name is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} creating new city: {CityName} for governorate {GovernorateId}", userId, request.Name, request.GovernorateId);

            try
            {
                // Validate governorate exists
                var governorate = await _unitOfWork.Governorates.GetByIdAsync(request.GovernorateId, cancellationToken);
                if (governorate == null)
                {
                    throw new InvalidOperationException($"Governorate with ID {request.GovernorateId} not found");
                }

                // Validate uniqueness within governorate
                if (await _unitOfWork.Cities.ExistsByNameAsync(request.Name, request.GovernorateId, cancellationToken))
                {
                    throw new InvalidOperationException($"A city with name '{request.Name}' already exists in {governorate.Name}");
                }

                // Create entity
                var city = new City
                {
                    GovernorateId = request.GovernorateId,
                    Name = request.Name,
                    NameArabic = request.NameArabic,
                    Code = request.Code,
                    SupportsSameDayDelivery = request.SupportsSameDayDelivery,
                    IsActive = true
                };

                var createdCity = await _unitOfWork.Cities.AddAsync(city, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully created city: {CityId}", createdCity.CityId);

                return new CityResponse
                {
                    CityId = createdCity.CityId,
                    GovernorateId = createdCity.GovernorateId,
                    GovernorateName = governorate.Name,
                    CountryName = governorate.Country?.Name ?? "Unknown",
                    Name = createdCity.Name,
                    NameArabic = createdCity.NameArabic,
                    Code = createdCity.Code,
                    IsActive = createdCity.IsActive,
                    SupportsSameDayDelivery = createdCity.SupportsSameDayDelivery,
                    CreatedAt = createdCity.CreatedAt,
                    UpdatedAt = createdCity.UpdatedAt
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
                _logger.LogError(ex, "Failed to create city: {CityName} for governorate {GovernorateId}", request.Name, request.GovernorateId);
                throw new InvalidOperationException($"Failed to create city '{request.Name}': {ex.Message}", ex);
            }
        }

        public async Task<CityResponse> UpdateCityAsync(CityUpdateRequest request, Guid userId, CancellationToken cancellationToken = default)
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

            if (request.CityId == Guid.Empty)
            {
                throw new ArgumentException("City ID cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("City name is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} updating city: {CityId}", userId, request.CityId);

            try
            {
                var existingCity = await _unitOfWork.Cities.GetByIdAsync(request.CityId, cancellationToken);
                if (existingCity == null)
                {
                    throw new InvalidOperationException($"City with ID {request.CityId} not found");
                }

                // Check name uniqueness within governorate (excluding current city)
                var cities = await _unitOfWork.Cities.GetByGovernorateIdAsync(existingCity.GovernorateId, cancellationToken);
                if (cities.Any(c => c.Name == request.Name && c.CityId != request.CityId))
                {
                    throw new InvalidOperationException($"A city with name '{request.Name}' already exists in this governorate");
                }

                // Update entity
                existingCity.Name = request.Name;
                existingCity.NameArabic = request.NameArabic;
                existingCity.Code = request.Code;
                existingCity.IsActive = request.IsActive;
                existingCity.SupportsSameDayDelivery = request.SupportsSameDayDelivery;

                var updatedCity = await _unitOfWork.Cities.UpdateAsync(existingCity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated city: {CityId}", updatedCity.CityId);

                return new CityResponse
                {
                    CityId = updatedCity.CityId,
                    GovernorateId = updatedCity.GovernorateId,
                    GovernorateName = updatedCity.Governorate?.Name ?? "Unknown",
                    CountryName = updatedCity.Governorate?.Country?.Name ?? "Unknown",
                    Name = updatedCity.Name,
                    NameArabic = updatedCity.NameArabic,
                    Code = updatedCity.Code,
                    IsActive = updatedCity.IsActive,
                    SupportsSameDayDelivery = updatedCity.SupportsSameDayDelivery,
                    CreatedAt = updatedCity.CreatedAt,
                    UpdatedAt = updatedCity.UpdatedAt
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
                _logger.LogError(ex, "Failed to update city: {CityId}", request.CityId);
                throw new InvalidOperationException($"Failed to update city {request.CityId}: {ex.Message}", ex);
            }
        }

        public async Task<CityResponse?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (cityId == Guid.Empty)
            {
                throw new ArgumentException("City ID cannot be empty", nameof(cityId));
            }

            try
            {
                var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);
                if (city == null)
                    return null;

                return new CityResponse
                {
                    CityId = city.CityId,
                    GovernorateId = city.GovernorateId,
                    GovernorateName = city.Governorate?.Name ?? "Unknown",
                    CountryName = city.Governorate?.Country?.Name ?? "Unknown",
                    Name = city.Name,
                    NameArabic = city.NameArabic,
                    Code = city.Code,
                    IsActive = city.IsActive,
                    SupportsSameDayDelivery = city.SupportsSameDayDelivery,
                    CreatedAt = city.CreatedAt,
                    UpdatedAt = city.UpdatedAt
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get city: {CityId}", cityId);
                throw new InvalidOperationException($"Failed to get city {cityId}: {ex.Message}", ex);
            }
        }

        public async Task<List<CityResponse>> GetCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            try
            {
                var cities = await _unitOfWork.Cities.GetByGovernorateIdAsync(governorateId, cancellationToken);

                return cities.Select(c => new CityResponse
                {
                    CityId = c.CityId,
                    GovernorateId = c.GovernorateId,
                    GovernorateName = c.Governorate?.Name ?? "Unknown",
                    CountryName = c.Governorate?.Country?.Name ?? "Unknown",
                    Name = c.Name,
                    NameArabic = c.NameArabic,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cities for governorate: {GovernorateId}", governorateId);
                throw new InvalidOperationException($"Failed to get cities for governorate {governorateId}: {ex.Message}", ex);
            }
        }

        public async Task<List<CityResponse>> GetActiveCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            try
            {
                var cities = await _unitOfWork.Cities.GetActiveByGovernorateIdAsync(governorateId, cancellationToken);

                return cities.Select(c => new CityResponse
                {
                    CityId = c.CityId,
                    GovernorateId = c.GovernorateId,
                    GovernorateName = c.Governorate?.Name ?? "Unknown",
                    CountryName = c.Governorate?.Country?.Name ?? "Unknown",
                    Name = c.Name,
                    NameArabic = c.NameArabic,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active cities for governorate: {GovernorateId}", governorateId);
                throw new InvalidOperationException($"Failed to get active cities for governorate {governorateId}: {ex.Message}", ex);
            }
        }

        public async Task<List<CityResponse>> GetAllCitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cities = await _unitOfWork.Cities.GetAllAsync(cancellationToken);

                return cities.Select(c => new CityResponse
                {
                    CityId = c.CityId,
                    GovernorateId = c.GovernorateId,
                    GovernorateName = c.Governorate?.Name ?? "Unknown",
                    CountryName = c.Governorate?.Country?.Name ?? "Unknown",
                    Name = c.Name,
                    NameArabic = c.NameArabic,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all cities");
                throw new InvalidOperationException($"Failed to get all cities: {ex.Message}", ex);
            }
        }

        public async Task<List<CityResponse>> SearchCitiesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term is required", nameof(searchTerm));
            }

            try
            {
                var cities = await _unitOfWork.Cities.SearchByNameAsync(searchTerm, cancellationToken);

                return cities.Select(c => new CityResponse
                {
                    CityId = c.CityId,
                    GovernorateId = c.GovernorateId,
                    GovernorateName = c.Governorate?.Name ?? "Unknown",
                    CountryName = c.Governorate?.Country?.Name ?? "Unknown",
                    Name = c.Name,
                    NameArabic = c.NameArabic,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search cities with term: {SearchTerm}", searchTerm);
                throw new InvalidOperationException($"Failed to search cities with term '{searchTerm}': {ex.Message}", ex);
            }
        }

        public async Task<List<CityResponse>> GetSameDayDeliveryCitiesAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            try
            {
                var cities = await _unitOfWork.Cities.GetSameDayDeliveryCitiesAsync(governorateId, cancellationToken);

                return cities.Select(c => new CityResponse
                {
                    CityId = c.CityId,
                    GovernorateId = c.GovernorateId,
                    GovernorateName = c.Governorate?.Name ?? "Unknown",
                    CountryName = c.Governorate?.Country?.Name ?? "Unknown",
                    Name = c.Name,
                    NameArabic = c.NameArabic,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get same-day delivery cities for governorate: {GovernorateId}", governorateId);
                throw new InvalidOperationException($"Failed to get same-day delivery cities for governorate {governorateId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeactivateCityAsync(Guid cityId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (cityId == Guid.Empty)
            {
                throw new ArgumentException("City ID cannot be empty", nameof(cityId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Validate admin privileges
            await _adminAuthService.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} deactivating city: {CityId}", userId, cityId);

            try
            {
                // Check if city exists first
                var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);
                if (city == null)
                {
                    throw new InvalidOperationException($"City with ID {cityId} not found");
                }

                var result = await _unitOfWork.Cities.DeactivateAsync(cityId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Successfully deactivated city: {CityId}", cityId);
                }
                else
                {
                    _logger.LogWarning("Failed to deactivate city: {CityId}", cityId);
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
                _logger.LogError(ex, "Failed to deactivate city: {CityId}", cityId);
                throw new InvalidOperationException($"Failed to deactivate city {cityId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("City name is required", nameof(name));
            }

            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            try
            {
                return await _unitOfWork.Cities.ExistsByNameAsync(name, governorateId, cancellationToken);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if city exists: {CityName} in governorate {GovernorateId}", name, governorateId);
                throw new InvalidOperationException($"Failed to check if city '{name}' exists: {ex.Message}", ex);
            }
        }
    }
}
