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

namespace Bazario.Core.Services.Location
{
    /// <summary>
    /// Service for managing city entities.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// </summary>
    public class CityManagementService : ICityManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CityManagementService> _logger;

        public CityManagementService(
            IUnitOfWork unitOfWork,
            ILogger<CityManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CityResponse> CreateCityAsync(CityAddRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new city: {CityName} for governorate {GovernorateId}", request.Name, request.GovernorateId);

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
                CountryName = governorate.Country.Name,
                Name = createdCity.Name,
                NameArabic = createdCity.NameArabic,
                Code = createdCity.Code,
                IsActive = createdCity.IsActive,
                SupportsSameDayDelivery = createdCity.SupportsSameDayDelivery,
                CreatedAt = createdCity.CreatedAt,
                UpdatedAt = createdCity.UpdatedAt
            };
        }

        public async Task<CityResponse> UpdateCityAsync(CityUpdateRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating city: {CityId}", request.CityId);

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
                GovernorateName = updatedCity.Governorate.Name,
                CountryName = updatedCity.Governorate.Country.Name,
                Name = updatedCity.Name,
                NameArabic = updatedCity.NameArabic,
                Code = updatedCity.Code,
                IsActive = updatedCity.IsActive,
                SupportsSameDayDelivery = updatedCity.SupportsSameDayDelivery,
                CreatedAt = updatedCity.CreatedAt,
                UpdatedAt = updatedCity.UpdatedAt
            };
        }

        public async Task<CityResponse?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);
            if (city == null)
                return null;

            return new CityResponse
            {
                CityId = city.CityId,
                GovernorateId = city.GovernorateId,
                GovernorateName = city.Governorate.Name,
                CountryName = city.Governorate.Country.Name,
                Name = city.Name,
                NameArabic = city.NameArabic,
                Code = city.Code,
                IsActive = city.IsActive,
                SupportsSameDayDelivery = city.SupportsSameDayDelivery,
                CreatedAt = city.CreatedAt,
                UpdatedAt = city.UpdatedAt
            };
        }

        public async Task<List<CityResponse>> GetCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            var cities = await _unitOfWork.Cities.GetByGovernorateIdAsync(governorateId, cancellationToken);

            return cities.Select(c => new CityResponse
            {
                CityId = c.CityId,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate.Name,
                CountryName = c.Governorate.Country.Name,
                Name = c.Name,
                NameArabic = c.NameArabic,
                Code = c.Code,
                IsActive = c.IsActive,
                SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<CityResponse>> GetActiveCitiesByGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            var cities = await _unitOfWork.Cities.GetActiveByGovernorateIdAsync(governorateId, cancellationToken);

            return cities.Select(c => new CityResponse
            {
                CityId = c.CityId,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate.Name,
                CountryName = c.Governorate.Country.Name,
                Name = c.Name,
                NameArabic = c.NameArabic,
                Code = c.Code,
                IsActive = c.IsActive,
                SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<CityResponse>> GetAllCitiesAsync(CancellationToken cancellationToken = default)
        {
            var cities = await _unitOfWork.Cities.GetAllAsync(cancellationToken);

            return cities.Select(c => new CityResponse
            {
                CityId = c.CityId,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate.Name,
                CountryName = c.Governorate.Country.Name,
                Name = c.Name,
                NameArabic = c.NameArabic,
                Code = c.Code,
                IsActive = c.IsActive,
                SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<CityResponse>> SearchCitiesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var cities = await _unitOfWork.Cities.SearchByNameAsync(searchTerm, cancellationToken);

            return cities.Select(c => new CityResponse
            {
                CityId = c.CityId,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate.Name,
                CountryName = c.Governorate.Country.Name,
                Name = c.Name,
                NameArabic = c.NameArabic,
                Code = c.Code,
                IsActive = c.IsActive,
                SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<CityResponse>> GetSameDayDeliveryCitiesAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            var cities = await _unitOfWork.Cities.GetSameDayDeliveryCitiesAsync(governorateId, cancellationToken);

            return cities.Select(c => new CityResponse
            {
                CityId = c.CityId,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate.Name,
                CountryName = c.Governorate.Country.Name,
                Name = c.Name,
                NameArabic = c.NameArabic,
                Code = c.Code,
                IsActive = c.IsActive,
                SupportsSameDayDelivery = c.SupportsSameDayDelivery,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<bool> DeactivateCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating city: {CityId}", cityId);

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

        public async Task<bool> ExistsByNameAsync(string name, Guid governorateId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Cities.ExistsByNameAsync(name, governorateId, cancellationToken);
        }
    }
}
