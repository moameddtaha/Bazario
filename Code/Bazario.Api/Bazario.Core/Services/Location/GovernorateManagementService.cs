using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.Governorate;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts;

namespace Bazario.Core.Services.Location
{
    /// <summary>
    /// Service for managing governorate entities.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// </summary>
    public class GovernorateManagementService : IGovernorateManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GovernorateManagementService> _logger;

        public GovernorateManagementService(
            IUnitOfWork unitOfWork,
            ILogger<GovernorateManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GovernorateResponse> CreateGovernorateAsync(GovernorateAddRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new governorate: {GovernorateName} for country {CountryId}", request.Name, request.CountryId);

            // Validate country exists
            var country = await _unitOfWork.Countries.GetByIdAsync(request.CountryId, cancellationToken);
            if (country == null)
            {
                throw new InvalidOperationException($"Country with ID {request.CountryId} not found");
            }

            // Validate uniqueness within country
            if (await _unitOfWork.Governorates.ExistsByNameInCountryAsync(request.Name, request.CountryId, cancellationToken))
            {
                throw new InvalidOperationException($"A governorate with name '{request.Name}' already exists in {country.Name}");
            }

            // Create entity
            var governorate = new Governorate
            {
                CountryId = request.CountryId,
                Name = request.Name,
                NameArabic = request.NameArabic,
                Code = request.Code,
                SupportsSameDayDelivery = request.SupportsSameDayDelivery,
                IsActive = true
            };

            var createdGovernorate = await _unitOfWork.Governorates.AddAsync(governorate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created governorate: {GovernorateId}", createdGovernorate.GovernorateId);

            return new GovernorateResponse
            {
                GovernorateId = createdGovernorate.GovernorateId,
                CountryId = createdGovernorate.CountryId,
                CountryName = country.Name,
                Name = createdGovernorate.Name,
                NameArabic = createdGovernorate.NameArabic,
                Code = createdGovernorate.Code,
                IsActive = createdGovernorate.IsActive,
                SupportsSameDayDelivery = createdGovernorate.SupportsSameDayDelivery,
                CityCount = 0,
                CreatedAt = createdGovernorate.CreatedAt,
                UpdatedAt = createdGovernorate.UpdatedAt
            };
        }

        public async Task<GovernorateResponse> UpdateGovernorateAsync(GovernorateUpdateRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating governorate: {GovernorateId}", request.GovernorateId);

            var existingGovernorate = await _unitOfWork.Governorates.GetByIdAsync(request.GovernorateId, cancellationToken);
            if (existingGovernorate == null)
            {
                throw new InvalidOperationException($"Governorate with ID {request.GovernorateId} not found");
            }

            // Check name uniqueness within country (excluding current governorate)
            var governorates = await _unitOfWork.Governorates.GetByCountryIdAsync(existingGovernorate.CountryId, cancellationToken);
            if (governorates.Any(g => g.Name == request.Name && g.GovernorateId != request.GovernorateId))
            {
                throw new InvalidOperationException($"A governorate with name '{request.Name}' already exists in this country");
            }

            // Update entity
            existingGovernorate.Name = request.Name;
            existingGovernorate.NameArabic = request.NameArabic;
            existingGovernorate.Code = request.Code;
            existingGovernorate.IsActive = request.IsActive;
            existingGovernorate.SupportsSameDayDelivery = request.SupportsSameDayDelivery;

            var updatedGovernorate = await _unitOfWork.Governorates.UpdateAsync(existingGovernorate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated governorate: {GovernorateId}", updatedGovernorate.GovernorateId);

            return new GovernorateResponse
            {
                GovernorateId = updatedGovernorate.GovernorateId,
                CountryId = updatedGovernorate.CountryId,
                CountryName = updatedGovernorate.Country.Name,
                Name = updatedGovernorate.Name,
                NameArabic = updatedGovernorate.NameArabic,
                Code = updatedGovernorate.Code,
                IsActive = updatedGovernorate.IsActive,
                SupportsSameDayDelivery = updatedGovernorate.SupportsSameDayDelivery,
                CityCount = updatedGovernorate.Cities?.Count ?? 0,
                CreatedAt = updatedGovernorate.CreatedAt,
                UpdatedAt = updatedGovernorate.UpdatedAt
            };
        }

        public async Task<GovernorateResponse?> GetGovernorateByIdAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            var governorate = await _unitOfWork.Governorates.GetByIdAsync(governorateId, cancellationToken);
            if (governorate == null)
                return null;

            return new GovernorateResponse
            {
                GovernorateId = governorate.GovernorateId,
                CountryId = governorate.CountryId,
                CountryName = governorate.Country.Name,
                Name = governorate.Name,
                NameArabic = governorate.NameArabic,
                Code = governorate.Code,
                IsActive = governorate.IsActive,
                SupportsSameDayDelivery = governorate.SupportsSameDayDelivery,
                CityCount = governorate.Cities?.Count ?? 0,
                CreatedAt = governorate.CreatedAt,
                UpdatedAt = governorate.UpdatedAt
            };
        }

        public async Task<List<GovernorateResponse>> GetGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            var governorates = await _unitOfWork.Governorates.GetByCountryIdAsync(countryId, cancellationToken);

            return governorates.Select(g => new GovernorateResponse
            {
                GovernorateId = g.GovernorateId,
                CountryId = g.CountryId,
                CountryName = g.Country.Name,
                Name = g.Name,
                NameArabic = g.NameArabic,
                Code = g.Code,
                IsActive = g.IsActive,
                SupportsSameDayDelivery = g.SupportsSameDayDelivery,
                CityCount = g.Cities?.Count ?? 0,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();
        }

        public async Task<List<GovernorateResponse>> GetActiveGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            var governorates = await _unitOfWork.Governorates.GetActiveByCountryIdAsync(countryId, cancellationToken);

            return governorates.Select(g => new GovernorateResponse
            {
                GovernorateId = g.GovernorateId,
                CountryId = g.CountryId,
                CountryName = g.Country.Name,
                Name = g.Name,
                NameArabic = g.NameArabic,
                Code = g.Code,
                IsActive = g.IsActive,
                SupportsSameDayDelivery = g.SupportsSameDayDelivery,
                CityCount = g.Cities?.Count ?? 0,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();
        }

        public async Task<List<GovernorateResponse>> GetAllGovernoratesAsync(CancellationToken cancellationToken = default)
        {
            var governorates = await _unitOfWork.Governorates.GetAllAsync(cancellationToken);

            return governorates.Select(g => new GovernorateResponse
            {
                GovernorateId = g.GovernorateId,
                CountryId = g.CountryId,
                CountryName = g.Country.Name,
                Name = g.Name,
                NameArabic = g.NameArabic,
                Code = g.Code,
                IsActive = g.IsActive,
                SupportsSameDayDelivery = g.SupportsSameDayDelivery,
                CityCount = g.Cities?.Count ?? 0,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();
        }

        public async Task<List<GovernorateResponse>> GetSameDayDeliveryGovernoratesAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            var governorates = await _unitOfWork.Governorates.GetSameDayDeliveryGovernoratesAsync(countryId, cancellationToken);

            return governorates.Select(g => new GovernorateResponse
            {
                GovernorateId = g.GovernorateId,
                CountryId = g.CountryId,
                CountryName = g.Country.Name,
                Name = g.Name,
                NameArabic = g.NameArabic,
                Code = g.Code,
                IsActive = g.IsActive,
                SupportsSameDayDelivery = g.SupportsSameDayDelivery,
                CityCount = g.Cities?.Count ?? 0,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();
        }

        public async Task<bool> DeactivateGovernorateAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating governorate: {GovernorateId}", governorateId);

            var result = await _unitOfWork.Governorates.DeactivateAsync(governorateId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully deactivated governorate: {GovernorateId}", governorateId);
            }
            else
            {
                _logger.LogWarning("Failed to deactivate governorate: {GovernorateId}", governorateId);
            }

            return result;
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid countryId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Governorates.ExistsByNameInCountryAsync(name, countryId, cancellationToken);
        }
    }
}
