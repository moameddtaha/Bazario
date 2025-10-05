using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Store;
using Bazario.Core.Helpers.Product;
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
        private readonly IProductValidationHelper _validationHelper;
        private readonly ILogger<StoreShippingConfigurationService> _logger;

        public StoreShippingConfigurationService(
            IStoreShippingConfigurationRepository configurationRepository,
            IStoreRepository storeRepository,
            IProductValidationHelper validationHelper,
            ILogger<StoreShippingConfigurationService> logger)
        {
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreShippingConfigurationResponse> GetConfigurationAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting shipping configuration for store: {StoreId}", storeId);

            try
            {
                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                
                if (configuration == null)
                {
                    _logger.LogWarning("No shipping configuration found for store: {StoreId}", storeId);
                    return new StoreShippingConfigurationResponse
                    {
                        StoreId = storeId,
                        DefaultShippingZone = Enums.ShippingZone.Local,
                        OffersSameDayDelivery = false,
                        OffersStandardDelivery = true,
                        IsActive = false
                    };
                }

                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);
                
                return new StoreShippingConfigurationResponse
                {
                    StoreId = storeId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<Enums.ShippingZone>(configuration.DefaultShippingZone, out var zone) ? zone : Enums.ShippingZone.Local,
                    OffersSameDayDelivery = configuration.OffersSameDayDelivery,
                    OffersStandardDelivery = configuration.OffersStandardDelivery,
                    SameDayCutoffHour = configuration.SameDayCutoffHour,
                    ShippingNotes = configuration.ShippingNotes,
                    SameDayDeliveryFee = configuration.SameDayDeliveryFee,
                    StandardDeliveryFee = configuration.StandardDeliveryFee,
                    NationalDeliveryFee = configuration.NationalDeliveryFee,
                    SupportedCities = configuration.SupportedCitiesList,
                    ExcludedCities = configuration.ExcludedCitiesList,
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

        public async Task<StoreShippingConfigurationResponse> CreateConfigurationAsync(StoreShippingConfigurationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating shipping configuration for store: {StoreId}", request.StoreId);

            try
            {
                // Validate store exists
                var store = await _storeRepository.GetStoreByIdAsync(request.StoreId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {request.StoreId} not found");
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
                    NationalDeliveryFee = request.NationalDeliveryFee,
                    SupportedCitiesList = request.SupportedCities ?? new List<string>(),
                    ExcludedCitiesList = request.ExcludedCities ?? new List<string>()
                };

                var createdConfiguration = await _configurationRepository.CreateAsync(configuration, cancellationToken);

                _logger.LogInformation("Successfully created shipping configuration for store: {StoreId}", request.StoreId);

                return new StoreShippingConfigurationResponse
                {
                    StoreId = createdConfiguration.StoreId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<Enums.ShippingZone>(createdConfiguration.DefaultShippingZone, out var createdZone) ? createdZone : Enums.ShippingZone.Local,
                    OffersSameDayDelivery = createdConfiguration.OffersSameDayDelivery,
                    OffersStandardDelivery = createdConfiguration.OffersStandardDelivery,
                    SameDayCutoffHour = createdConfiguration.SameDayCutoffHour,
                    ShippingNotes = createdConfiguration.ShippingNotes,
                    SameDayDeliveryFee = createdConfiguration.SameDayDeliveryFee,
                    StandardDeliveryFee = createdConfiguration.StandardDeliveryFee,
                    NationalDeliveryFee = createdConfiguration.NationalDeliveryFee,
                    SupportedCities = createdConfiguration.SupportedCitiesList,
                    ExcludedCities = createdConfiguration.ExcludedCitiesList,
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

        public async Task<StoreShippingConfigurationResponse> UpdateConfigurationAsync(StoreShippingConfigurationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating shipping configuration for store: {StoreId}", request.StoreId);

            try
            {
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
                existingConfiguration.SupportedCitiesList = request.SupportedCities ?? new List<string>();
                existingConfiguration.ExcludedCitiesList = request.ExcludedCities ?? new List<string>();

                var updatedConfiguration = await _configurationRepository.UpdateAsync(existingConfiguration, cancellationToken);

                var store = await _storeRepository.GetStoreByIdAsync(request.StoreId, cancellationToken);

                _logger.LogInformation("Successfully updated shipping configuration for store: {StoreId}", request.StoreId);

                return new StoreShippingConfigurationResponse
                {
                    StoreId = updatedConfiguration.StoreId,
                    StoreName = store?.Name ?? "Unknown Store",
                    DefaultShippingZone = Enum.TryParse<Enums.ShippingZone>(updatedConfiguration.DefaultShippingZone, out var updatedZone) ? updatedZone : Enums.ShippingZone.Local,
                    OffersSameDayDelivery = updatedConfiguration.OffersSameDayDelivery,
                    OffersStandardDelivery = updatedConfiguration.OffersStandardDelivery,
                    SameDayCutoffHour = updatedConfiguration.SameDayCutoffHour,
                    ShippingNotes = updatedConfiguration.ShippingNotes,
                    SameDayDeliveryFee = updatedConfiguration.SameDayDeliveryFee,
                    StandardDeliveryFee = updatedConfiguration.StandardDeliveryFee,
                    NationalDeliveryFee = updatedConfiguration.NationalDeliveryFee,
                    SupportedCities = updatedConfiguration.SupportedCitiesList,
                    ExcludedCities = updatedConfiguration.ExcludedCitiesList,
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
                if (!await _validationHelper.HasAdminPrivilegesAsync(deletedBy, cancellationToken))
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
                var configuration = await _configurationRepository.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null || !configuration.OffersSameDayDelivery)
                {
                    return false;
                }

                // Check if city is excluded
                if (configuration.ExcludedCitiesList.Contains(city.ToUpperInvariant()))
                {
                    return false;
                }

                // Check if city is in supported cities (if specified)
                if (configuration.SupportedCitiesList.Any() && !configuration.SupportedCitiesList.Contains(city.ToUpperInvariant()))
                {
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
