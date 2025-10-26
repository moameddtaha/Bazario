using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.DTO.Location.Governorate;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.Extensions.Logging;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Helpers.Authorization;

namespace Bazario.Core.Services.Location
{
    /// <summary>
    /// Service for managing governorate entities.
    /// Uses Unit of Work pattern for transaction management and data consistency.
    /// Requires admin privileges for write operations (Create, Update, Deactivate).
    /// </summary>
    public class GovernorateManagementService : IGovernorateManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminAuthorizationHelper _adminAuthHelper;
        private readonly ILogger<GovernorateManagementService> _logger;

        public GovernorateManagementService(
            IUnitOfWork unitOfWork,
            IAdminAuthorizationHelper adminAuthHelper,
            ILogger<GovernorateManagementService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _adminAuthHelper = adminAuthHelper ?? throw new ArgumentNullException(nameof(adminAuthHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GovernorateResponse> CreateGovernorateAsync(GovernorateAddRequest request, Guid userId, CancellationToken cancellationToken = default)
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
                throw new ArgumentException("Governorate name is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthHelper.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} creating new governorate: {GovernorateName} for country {CountryId}", userId, request.Name, request.CountryId);

            try
            {
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
                _logger.LogError(ex, "Failed to create governorate: {GovernorateName}", request.Name);
                throw new InvalidOperationException($"Failed to create governorate '{request.Name}': {ex.Message}", ex);
            }
        }

        public async Task<GovernorateResponse> UpdateGovernorateAsync(GovernorateUpdateRequest request, Guid userId, CancellationToken cancellationToken = default)
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
                throw new ArgumentException("Governorate name is required", nameof(request));
            }

            // Validate admin privileges
            await _adminAuthHelper.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} updating governorate: {GovernorateId}", userId, request.GovernorateId);

            try
            {
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
                    CountryName = updatedGovernorate.Country?.Name ?? "Unknown",
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
                _logger.LogError(ex, "Failed to update governorate: {GovernorateId}", request.GovernorateId);
                throw new InvalidOperationException($"Failed to update governorate {request.GovernorateId}: {ex.Message}", ex);
            }
        }

        public async Task<GovernorateResponse?> GetGovernorateByIdAsync(Guid governorateId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            try
            {
                var governorate = await _unitOfWork.Governorates.GetByIdAsync(governorateId, cancellationToken);
                if (governorate == null)
                    return null;

                return new GovernorateResponse
                {
                    GovernorateId = governorate.GovernorateId,
                    CountryId = governorate.CountryId,
                    CountryName = governorate.Country?.Name ?? "Unknown",
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get governorate: {GovernorateId}", governorateId);
                throw new InvalidOperationException($"Failed to get governorate {governorateId}: {ex.Message}", ex);
            }
        }

        public async Task<List<GovernorateResponse>> GetGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            try
            {
                var governorates = await _unitOfWork.Governorates.GetByCountryIdAsync(countryId, cancellationToken);

                return governorates.Select(g => new GovernorateResponse
                {
                    GovernorateId = g.GovernorateId,
                    CountryId = g.CountryId,
                    CountryName = g.Country?.Name ?? "Unknown",
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get governorates for country: {CountryId}", countryId);
                throw new InvalidOperationException($"Failed to get governorates for country {countryId}: {ex.Message}", ex);
            }
        }

        public async Task<List<GovernorateResponse>> GetActiveGovernoratesByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            try
            {
                var governorates = await _unitOfWork.Governorates.GetActiveByCountryIdAsync(countryId, cancellationToken);

                return governorates.Select(g => new GovernorateResponse
                {
                    GovernorateId = g.GovernorateId,
                    CountryId = g.CountryId,
                    CountryName = g.Country?.Name ?? "Unknown",
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active governorates for country: {CountryId}", countryId);
                throw new InvalidOperationException($"Failed to get active governorates for country {countryId}: {ex.Message}", ex);
            }
        }

        public async Task<List<GovernorateResponse>> GetAllGovernoratesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var governorates = await _unitOfWork.Governorates.GetAllAsync(cancellationToken);

                return governorates.Select(g => new GovernorateResponse
                {
                    GovernorateId = g.GovernorateId,
                    CountryId = g.CountryId,
                    CountryName = g.Country?.Name ?? "Unknown",
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all governorates");
                throw new InvalidOperationException($"Failed to get all governorates: {ex.Message}", ex);
            }
        }

        public async Task<List<GovernorateResponse>> GetSameDayDeliveryGovernoratesAsync(Guid countryId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            try
            {
                var governorates = await _unitOfWork.Governorates.GetSameDayDeliveryGovernoratesAsync(countryId, cancellationToken);

                return governorates.Select(g => new GovernorateResponse
                {
                    GovernorateId = g.GovernorateId,
                    CountryId = g.CountryId,
                    CountryName = g.Country?.Name ?? "Unknown",
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
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get same-day delivery governorates for country: {CountryId}", countryId);
                throw new InvalidOperationException($"Failed to get same-day delivery governorates for country {countryId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeactivateGovernorateAsync(Guid governorateId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (governorateId == Guid.Empty)
            {
                throw new ArgumentException("Governorate ID cannot be empty", nameof(governorateId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Validate admin privileges
            await _adminAuthHelper.ValidateAdminPrivilegesAsync(userId, cancellationToken);

            _logger.LogInformation("User {UserId} deactivating governorate: {GovernorateId}", userId, governorateId);

            try
            {
                // Check if governorate exists first
                var governorate = await _unitOfWork.Governorates.GetByIdAsync(governorateId, cancellationToken);
                if (governorate == null)
                {
                    throw new InvalidOperationException($"Governorate with ID {governorateId} not found");
                }

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
                _logger.LogError(ex, "Failed to deactivate governorate: {GovernorateId}", governorateId);
                throw new InvalidOperationException($"Failed to deactivate governorate {governorateId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid countryId, CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Governorate name is required", nameof(name));
            }

            if (countryId == Guid.Empty)
            {
                throw new ArgumentException("Country ID cannot be empty", nameof(countryId));
            }

            try
            {
                return await _unitOfWork.Governorates.ExistsByNameInCountryAsync(name, countryId, cancellationToken);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if governorate exists: {GovernorateName} in country {CountryId}", name, countryId);
                throw new InvalidOperationException($"Failed to check if governorate '{name}' exists: {ex.Message}", ex);
            }
        }
    }
}
