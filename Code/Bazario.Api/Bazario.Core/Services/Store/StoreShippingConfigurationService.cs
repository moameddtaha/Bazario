using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.Entities.Store;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.DTO.Store;
using Bazario.Core.Enums.Order;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store shipping configuration operations
    /// </summary>
    public class StoreShippingConfigurationService : IStoreShippingConfigurationService
    {
        private readonly IStoreShippingConfigurationRepository _configurationRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IStoreAuthorizationService _authorizationService;
        private readonly IStoreGovernorateSupportRepository _governorateSupportRepository;
        private readonly ICityRepository _cityRepository;
        private readonly ILogger<StoreShippingConfigurationService> _logger;

        public StoreShippingConfigurationService(
            IStoreShippingConfigurationRepository configurationRepository,
            IStoreRepository storeRepository,
            IStoreAuthorizationService authorizationService,
            IStoreGovernorateSupportRepository governorateSupportRepository,
            ICityRepository cityRepository,
            ILogger<StoreShippingConfigurationService> logger)
        {
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _governorateSupportRepository = governorateSupportRepository ?? throw new ArgumentNullException(nameof(governorateSupportRepository));
            _cityRepository = cityRepository ?? throw new ArgumentNullException(nameof(cityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Maps a collection of StoreGovernorateSupport entities to GovernorateShippingInfo DTOs
        /// </summary>
        private static List<GovernorateShippingInfo> MapGovernorateShippingInfo(IEnumerable<StoreGovernorateSupport> governorates) =>
            [.. governorates.Select(sg => new GovernorateShippingInfo
            {
                GovernorateId = sg.Governorate.GovernorateId,
                GovernorateName = sg.Governorate.Name,
                GovernorateNameArabic = sg.Governorate.NameArabic,
                CountryId = sg.Governorate.CountryId,
                CountryName = sg.Governorate.Country.Name,
                SupportsSameDayDelivery = sg.Governorate.SupportsSameDayDelivery
            })];

        /// <summary>
        /// Validates the shipping configuration request for business rules
        /// </summary>
        private void ValidateConfigurationBusinessRules(StoreShippingConfigurationRequest request)
        {
            // Validate no conflicting governorates (can't be both supported AND excluded)
            if (request.SupportedGovernorateIds != null && request.ExcludedGovernorateIds != null)
            {
                var conflicts = request.SupportedGovernorateIds.Intersect(request.ExcludedGovernorateIds).ToList();
                if (conflicts.Count > 0)
                {
                    var conflictIds = string.Join(", ", conflicts);
                    throw new ArgumentException($"Governorates cannot be both supported and excluded. Conflicting IDs: {conflictIds}");
                }
            }

            // Validate same-day delivery requires cutoff hour
            if (request.OffersSameDayDelivery && !request.SameDayCutoffHour.HasValue)
            {
                throw new ArgumentException("Same-day delivery requires a cutoff hour to be specified");
            }

            // Validate same-day delivery requires at least some supported governorates
            if (request.OffersSameDayDelivery &&
                (request.SupportedGovernorateIds == null || request.SupportedGovernorateIds.Count == 0))
            {
                throw new ArgumentException("Same-day delivery requires at least one supported governorate");
            }

            // Validate pricing logic - same-day should be >= standard (warning via log)
            if (request.OffersSameDayDelivery &&
                request.SameDayDeliveryFee < request.StandardDeliveryFee)
            {
                _logger.LogWarning("Same-day delivery fee ({SameDayFee}) is less than standard delivery fee ({StandardFee}). This is unusual pricing.",
                    request.SameDayDeliveryFee, request.StandardDeliveryFee);
            }

            // Validate at least one delivery option is offered
            if (!request.OffersSameDayDelivery && !request.OffersStandardDelivery)
            {
                throw new ArgumentException("At least one delivery option (same-day or standard) must be offered");
            }
        }

        public async Task<StoreShippingConfigurationResponse> GetConfigurationAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting shipping configuration for store: {StoreId}", storeId);

            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                
                if (configuration == null)
                {
                    _logger.LogWarning("No shipping configuration found for store: {StoreId}", storeId);
                    return new StoreShippingConfigurationResponse
                    {
                        StoreId = storeId,
                        DefaultShippingZone = ShippingZone.Local,
                        OffersSameDayDelivery = false,
                        OffersStandardDelivery = true,
                        IsActive = false
                    };
                }

                // Parallelize independent database queries for better performance
                var storeTask = _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                var supportedGovernoratesTask = _governorateSupportRepository.GetSupportedGovernorates(storeId, cancellationToken);
                var excludedGovernoratesTask = _governorateSupportRepository.GetExcludedGovernorates(storeId, cancellationToken);

                await Task.WhenAll(storeTask, supportedGovernoratesTask, excludedGovernoratesTask);

                var store = await storeTask;
                var supportedGovernorates = await supportedGovernoratesTask;
                var excludedGovernorates = await excludedGovernoratesTask;

                return new StoreShippingConfigurationResponse
                {
                    StoreId = storeId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<ShippingZone>(configuration.DefaultShippingZone, out var zone) ? zone : ShippingZone.Local,
                    OffersSameDayDelivery = configuration.OffersSameDayDelivery,
                    OffersStandardDelivery = configuration.OffersStandardDelivery,
                    SameDayCutoffHour = configuration.SameDayCutoffHour,
                    ShippingNotes = configuration.ShippingNotes,
                    SameDayDeliveryFee = configuration.SameDayDeliveryFee,
                    StandardDeliveryFee = configuration.StandardDeliveryFee,
                    NationalDeliveryFee = configuration.NationalDeliveryFee,
                    SupportedGovernorates = MapGovernorateShippingInfo(supportedGovernorates),
                    ExcludedGovernorates = MapGovernorateShippingInfo(excludedGovernorates),
                    CreatedAt = configuration.CreatedAt,
                    UpdatedAt = configuration.UpdatedAt,
                    IsActive = configuration.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get shipping configuration for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<StoreShippingConfigurationResponse> CreateConfigurationAsync(StoreShippingConfigurationRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating shipping configuration for store: {StoreId} by user: {UserId}", request?.StoreId, userId);

            try
            {
                // Validate input
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Configuration request cannot be null");
                }

                if (request.StoreId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(request));
                }

                if (userId == Guid.Empty)
                {
                    throw new ArgumentException("User ID cannot be empty", nameof(userId));
                }

                // Validate business rules
                ValidateConfigurationBusinessRules(request);

                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(request.StoreId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {request.StoreId} not found");
                }

                // Check authorization - user must be store owner or admin
                var canManage = await _authorizationService.CanUserManageStoreAsync(userId, request.StoreId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("User {UserId} attempted to create shipping configuration for store {StoreId} without authorization", userId, request.StoreId);
                    throw new UnauthorizedAccessException($"User is not authorized to create shipping configuration for store {request.StoreId}");
                }

                // Check if configuration already exists
                var existingConfig = await _configurationRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
                if (existingConfig != null)
                {
                    throw new InvalidOperationException($"Shipping configuration already exists for store {request.StoreId}");
                }

                // Create new configuration
                var configuration = new StoreShippingConfiguration
                {
                    StoreId = request.StoreId,
                    DefaultShippingZone = request.DefaultShippingZone.ToString(),
                    OffersSameDayDelivery = request.OffersSameDayDelivery,
                    OffersStandardDelivery = request.OffersStandardDelivery,
                    SameDayCutoffHour = request.SameDayCutoffHour,
                    ShippingNotes = request.ShippingNotes,
                    SameDayDeliveryFee = request.SameDayDeliveryFee,
                    StandardDeliveryFee = request.StandardDeliveryFee,
                    NationalDeliveryFee = request.NationalDeliveryFee
                };

                var createdConfiguration = await _configurationRepository.CreateAsync(configuration, cancellationToken);

                // Create junction table records for governorates
                if (request.SupportedGovernorateIds != null && request.SupportedGovernorateIds.Any())
                {
                    var supportedRecords = request.SupportedGovernorateIds.Select(govId => new StoreGovernorateSupport
                    {
                        StoreId = request.StoreId,
                        GovernorateId = govId,
                        IsSupported = true
                    }).ToList();

                    await _governorateSupportRepository.AddRangeAsync(supportedRecords, cancellationToken);
                }

                if (request.ExcludedGovernorateIds != null && request.ExcludedGovernorateIds.Any())
                {
                    var excludedRecords = request.ExcludedGovernorateIds.Select(govId => new StoreGovernorateSupport
                    {
                        StoreId = request.StoreId,
                        GovernorateId = govId,
                        IsSupported = false
                    }).ToList();

                    await _governorateSupportRepository.AddRangeAsync(excludedRecords, cancellationToken);
                }

                _logger.LogInformation("Successfully created shipping configuration for store: {StoreId}", request.StoreId);

                // Get governorate data for response - parallelize for better performance
                var supportedGovernoratesTask = _governorateSupportRepository.GetSupportedGovernorates(request.StoreId, cancellationToken);
                var excludedGovernoratesTask = _governorateSupportRepository.GetExcludedGovernorates(request.StoreId, cancellationToken);

                await Task.WhenAll(supportedGovernoratesTask, excludedGovernoratesTask);

                var supportedGovernorates = await supportedGovernoratesTask;
                var excludedGovernorates = await excludedGovernoratesTask;

                return new StoreShippingConfigurationResponse
                {
                    StoreId = createdConfiguration.StoreId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<ShippingZone>(createdConfiguration.DefaultShippingZone, out var createdZone) ? createdZone : ShippingZone.Local,
                    OffersSameDayDelivery = createdConfiguration.OffersSameDayDelivery,
                    OffersStandardDelivery = createdConfiguration.OffersStandardDelivery,
                    SameDayCutoffHour = createdConfiguration.SameDayCutoffHour,
                    ShippingNotes = createdConfiguration.ShippingNotes,
                    SameDayDeliveryFee = createdConfiguration.SameDayDeliveryFee,
                    StandardDeliveryFee = createdConfiguration.StandardDeliveryFee,
                    NationalDeliveryFee = createdConfiguration.NationalDeliveryFee,
                    SupportedGovernorates = MapGovernorateShippingInfo(supportedGovernorates),
                    ExcludedGovernorates = MapGovernorateShippingInfo(excludedGovernorates),
                    CreatedAt = createdConfiguration.CreatedAt,
                    UpdatedAt = createdConfiguration.UpdatedAt,
                    IsActive = createdConfiguration.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shipping configuration for store: {StoreId}", request.StoreId);
                throw;
            }
        }

        public async Task<StoreShippingConfigurationResponse> UpdateConfigurationAsync(StoreShippingConfigurationRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating shipping configuration for store: {StoreId} by user: {UserId}", request?.StoreId, userId);

            try
            {
                // Validate input
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Configuration request cannot be null");
                }

                if (request.StoreId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(request));
                }

                if (userId == Guid.Empty)
                {
                    throw new ArgumentException("User ID cannot be empty", nameof(userId));
                }

                // Validate business rules
                ValidateConfigurationBusinessRules(request);

                // Check authorization - user must be store owner or admin
                var canManage = await _authorizationService.CanUserManageStoreAsync(userId, request.StoreId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("User {UserId} attempted to update shipping configuration for store {StoreId} without authorization", userId, request.StoreId);
                    throw new UnauthorizedAccessException($"User is not authorized to update shipping configuration for store {request.StoreId}");
                }

                // Get existing configuration
                var existingConfiguration = await _configurationRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
                if (existingConfiguration == null)
                {
                    throw new InvalidOperationException($"No shipping configuration found for store {request.StoreId}");
                }

                // Update configuration
                existingConfiguration.DefaultShippingZone = request.DefaultShippingZone.ToString();
                existingConfiguration.OffersSameDayDelivery = request.OffersSameDayDelivery;
                existingConfiguration.OffersStandardDelivery = request.OffersStandardDelivery;
                existingConfiguration.SameDayCutoffHour = request.SameDayCutoffHour;
                existingConfiguration.ShippingNotes = request.ShippingNotes;
                existingConfiguration.SameDayDeliveryFee = request.SameDayDeliveryFee;
                existingConfiguration.StandardDeliveryFee = request.StandardDeliveryFee;
                existingConfiguration.NationalDeliveryFee = request.NationalDeliveryFee;

                var updatedConfiguration = await _configurationRepository.UpdateAsync(existingConfiguration, cancellationToken);

                // Update junction table records for governorates using replace strategy
                var newGovernorateRecords = new List<StoreGovernorateSupport>();

                if (request.SupportedGovernorateIds != null && request.SupportedGovernorateIds.Any())
                {
                    newGovernorateRecords.AddRange(request.SupportedGovernorateIds.Select(govId => new StoreGovernorateSupport
                    {
                        StoreId = request.StoreId,
                        GovernorateId = govId,
                        IsSupported = true
                    }));
                }

                if (request.ExcludedGovernorateIds != null && request.ExcludedGovernorateIds.Any())
                {
                    newGovernorateRecords.AddRange(request.ExcludedGovernorateIds.Select(govId => new StoreGovernorateSupport
                    {
                        StoreId = request.StoreId,
                        GovernorateId = govId,
                        IsSupported = false
                    }));
                }

                await _governorateSupportRepository.ReplaceStoreGovernorates(request.StoreId, newGovernorateRecords, cancellationToken);

                // Get updated governorate data and store for response - parallelize for better performance
                var storeTask = _storeRepository.GetStoreByIdAsync(request.StoreId, cancellationToken);
                var supportedGovernoratesTask = _governorateSupportRepository.GetSupportedGovernorates(request.StoreId, cancellationToken);
                var excludedGovernoratesTask = _governorateSupportRepository.GetExcludedGovernorates(request.StoreId, cancellationToken);

                await Task.WhenAll(storeTask, supportedGovernoratesTask, excludedGovernoratesTask);

                var store = await storeTask;
                var supportedGovernorates = await supportedGovernoratesTask;
                var excludedGovernorates = await excludedGovernoratesTask;

                _logger.LogInformation("Successfully updated shipping configuration for store: {StoreId}", request.StoreId);

                return new StoreShippingConfigurationResponse
                {
                    StoreId = updatedConfiguration.StoreId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<ShippingZone>(updatedConfiguration.DefaultShippingZone, out var updatedZone) ? updatedZone : ShippingZone.Local,
                    OffersSameDayDelivery = updatedConfiguration.OffersSameDayDelivery,
                    OffersStandardDelivery = updatedConfiguration.OffersStandardDelivery,
                    SameDayCutoffHour = updatedConfiguration.SameDayCutoffHour,
                    ShippingNotes = updatedConfiguration.ShippingNotes,
                    SameDayDeliveryFee = updatedConfiguration.SameDayDeliveryFee,
                    StandardDeliveryFee = updatedConfiguration.StandardDeliveryFee,
                    NationalDeliveryFee = updatedConfiguration.NationalDeliveryFee,
                    SupportedGovernorates = MapGovernorateShippingInfo(supportedGovernorates),
                    ExcludedGovernorates = MapGovernorateShippingInfo(excludedGovernorates),
                    CreatedAt = updatedConfiguration.CreatedAt,
                    UpdatedAt = updatedConfiguration.UpdatedAt,
                    IsActive = updatedConfiguration.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shipping configuration for store: {StoreId}", request.StoreId);
                throw;
            }
        }

        public async Task<bool> DeleteConfigurationAsync(Guid storeId, Guid deletedBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin user {DeletedBy} attempting to delete shipping configuration for store: {StoreId}, Reason: {Reason}", deletedBy, storeId, reason);

            try
            {
                // Validate inputs
                if (storeId == Guid.Empty)
                {
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                if (deletedBy == Guid.Empty)
                {
                    throw new ArgumentException("DeletedBy user ID cannot be empty", nameof(deletedBy));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Reason is required for deletion", nameof(reason));
                }

                // Check if user has admin privileges
                var isAdmin = await _authorizationService.IsUserAdminAsync(deletedBy, cancellationToken);
                if (!isAdmin)
                {
                    _logger.LogWarning("User {UserId} attempted to delete shipping configuration for store {StoreId} without admin privileges", deletedBy, storeId);
                    throw new UnauthorizedAccessException("Only administrators can delete shipping configurations");
                }

                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null)
                {
                    _logger.LogWarning("No shipping configuration found for store: {StoreId}", storeId);
                    return false;
                }

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. StoreId: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                    storeId, deletedBy, reason);

                var result = await _configurationRepository.DeleteAsync(configuration.ConfigurationId, cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("Successfully deleted shipping configuration for store: {StoreId} by admin user: {DeletedBy}", storeId, deletedBy);
                }
                else
                {
                    _logger.LogWarning("Failed to delete shipping configuration for store: {StoreId}", storeId);
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while deleting shipping configuration for store: {StoreId}", storeId);
                throw; // Re-throw argument exceptions as-is
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Authorization error while deleting shipping configuration for store: {StoreId}", storeId);
                throw; // Re-throw authorization exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shipping configuration for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<bool> IsSameDayDeliveryAvailableAsync(Guid storeId, string city, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking same-day delivery availability for store: {StoreId}, city: {City}", storeId, city);

            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("IsSameDayDeliveryAvailableAsync called with empty store ID");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    _logger.LogWarning("IsSameDayDeliveryAvailableAsync called with empty city for store: {StoreId}", storeId);
                    return false;
                }

                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null || !configuration.OffersSameDayDelivery)
                {
                    return false;
                }

                // Resolve city to governorate using database lookup
                var cities = await _cityRepository.SearchByNameAsync(city, cancellationToken);
                var cityEntity = cities.FirstOrDefault(c => c.Name.Equals(city, StringComparison.OrdinalIgnoreCase));

                if (cityEntity == null)
                {
                    _logger.LogDebug("City {City} not found in database, same-day delivery unavailable", city);
                    return false;
                }

                // Check if store supports this governorate
                var isSupported = await _governorateSupportRepository.IsGovernorateSupportedAsync(storeId, cityEntity.GovernorateId, cancellationToken);
                if (!isSupported)
                {
                    _logger.LogDebug("Store {StoreId} does not support governorate {GovernorateId} for city {City}", storeId, cityEntity.GovernorateId, city);
                    return false;
                }

                // Check if the city's governorate supports same-day delivery
                if (!cityEntity.Governorate.SupportsSameDayDelivery)
                {
                    _logger.LogDebug("Governorate {GovernorateName} does not support same-day delivery", cityEntity.Governorate.Name);
                    return false;
                }

                // Check cutoff time
                if (configuration.SameDayCutoffHour.HasValue)
                {
                    var currentHour = DateTime.UtcNow.Hour;
                    if (currentHour > configuration.SameDayCutoffHour.Value)
                    {
                        _logger.LogDebug("Same-day delivery cutoff time passed for store: {StoreId}", storeId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check same-day delivery availability for store: {StoreId}, city: {City}", storeId, city);
                return false;
            }
        }


        public async Task<decimal> GetDeliveryFeeAsync(Guid storeId, string city, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting delivery fee for store: {StoreId}, city: {City}", storeId, city);

            try
            {
                // Validate input
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("GetDeliveryFeeAsync called with empty store ID");
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    _logger.LogWarning("GetDeliveryFeeAsync called with empty city for store: {StoreId}", storeId);
                    return 0;
                }

                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null)
                {
                    return 0; // No configuration means no delivery fee
                }

                // Check for same-day delivery
                if (await IsSameDayDeliveryAvailableAsync(storeId, city, cancellationToken))
                {
                    return configuration.SameDayDeliveryFee;
                }


                // Standard delivery
                return configuration.StandardDeliveryFee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get delivery fee for store: {StoreId}, city: {City}", storeId, city);
                return 0;
            }
        }
    }
}
