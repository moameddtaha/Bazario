using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.Domain.Entities.Store;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.DTO.Store;
using Bazario.Core.Enums.Order;
using Bazario.Core.Helpers.Catalog;
using Bazario.Core.Helpers.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store shipping configuration operations
    /// Uses Unit of Work pattern for transaction management and data consistency
    /// </summary>
    public class StoreShippingConfigurationService : IStoreShippingConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreAuthorizationService _authorizationService;
        private readonly IConcurrencyHelper _concurrencyHelper;
        private readonly IStoreShippingConfigurationHelper _helper;
        private readonly ILogger<StoreShippingConfigurationService> _logger;

        public StoreShippingConfigurationService(
            IUnitOfWork unitOfWork,
            IStoreAuthorizationService authorizationService,
            IConcurrencyHelper concurrencyHelper,
            IStoreShippingConfigurationHelper helper,
            ILogger<StoreShippingConfigurationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _concurrencyHelper = concurrencyHelper ?? throw new ArgumentNullException(nameof(concurrencyHelper));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                var configuration = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(storeId, cancellationToken);
                
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
                var storeTask = _unitOfWork.Stores.GetStoreByIdAsync(storeId, cancellationToken);
                var supportedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetSupportedGovernorates(storeId, cancellationToken);
                var excludedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetExcludedGovernorates(storeId, cancellationToken);

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
                    SupportedGovernorates = _helper.MapGovernorateShippingInfo(supportedGovernorates),
                    ExcludedGovernorates = _helper.MapGovernorateShippingInfo(excludedGovernorates),
                    CreatedAt = configuration.CreatedAt,
                    UpdatedAt = configuration.UpdatedAt,
                    IsActive = configuration.IsActive,
                    RowVersion = configuration.RowVersion
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

                // Check authorization FIRST - user must be store owner or admin
                // This prevents unauthorized users from probing business rules through validation errors
                var canManage = await _authorizationService.CanUserManageStoreAsync(userId, request.StoreId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("User {UserId} attempted to create shipping configuration for store {StoreId} without authorization", userId, request.StoreId);
                    throw new UnauthorizedAccessException($"User is not authorized to create shipping configuration for store {request.StoreId}");
                }

                // Validate store exists
                var store = await _unitOfWork.Stores.GetStoreByIdAsync(request.StoreId, cancellationToken);
                if (store == null)
                {
                    throw new InvalidOperationException($"Store with ID {request.StoreId} not found");
                }

                // Validate business rules (after authorization check)
                _helper.ValidateConfigurationBusinessRules(request);

                // Check if configuration already exists
                var existingConfig = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(request.StoreId, cancellationToken);
                if (existingConfig != null)
                {
                    throw new InvalidOperationException($"Shipping configuration already exists for store {request.StoreId}");
                }

                // Use Unit of Work transaction to ensure atomic operation
                // Configuration and governorates are created together or not at all
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
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

                    var createdConfiguration = await _unitOfWork.StoreShippingConfigurations.CreateAsync(configuration, cancellationToken);

                    // Create junction table records for governorates
                    if (request.SupportedGovernorateIds != null && request.SupportedGovernorateIds.Any())
                    {
                        var supportedRecords = request.SupportedGovernorateIds.Select(govId => new StoreGovernorateSupport
                        {
                            StoreId = request.StoreId,
                            GovernorateId = govId,
                            IsSupported = true
                        }).ToList();

                        await _unitOfWork.StoreGovernorateSupports.AddRangeAsync(supportedRecords, cancellationToken);
                    }

                    if (request.ExcludedGovernorateIds != null && request.ExcludedGovernorateIds.Any())
                    {
                        var excludedRecords = request.ExcludedGovernorateIds.Select(govId => new StoreGovernorateSupport
                        {
                            StoreId = request.StoreId,
                            GovernorateId = govId,
                            IsSupported = false
                        }).ToList();

                        await _unitOfWork.StoreGovernorateSupports.AddRangeAsync(excludedRecords, cancellationToken);
                    }

                    // Commit transaction - all changes are saved atomically
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Successfully created shipping configuration for store: {StoreId}", request.StoreId);

                    // Get governorate data for response - parallelize for better performance
                    var supportedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetSupportedGovernorates(request.StoreId, cancellationToken);
                    var excludedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetExcludedGovernorates(request.StoreId, cancellationToken);

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
                        SupportedGovernorates = _helper.MapGovernorateShippingInfo(supportedGovernorates),
                        ExcludedGovernorates = _helper.MapGovernorateShippingInfo(excludedGovernorates),
                        CreatedAt = createdConfiguration.CreatedAt,
                        UpdatedAt = createdConfiguration.UpdatedAt,
                        IsActive = createdConfiguration.IsActive,
                        RowVersion = createdConfiguration.RowVersion
                    };
                }
                catch
                {
                    // Rollback transaction on any error - ensures data consistency
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shipping configuration for store: {StoreId}", request?.StoreId);
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

                // Check authorization FIRST - user must be store owner or admin
                // This prevents unauthorized users from probing business rules through validation errors
                var canManage = await _authorizationService.CanUserManageStoreAsync(userId, request.StoreId, cancellationToken);
                if (!canManage)
                {
                    _logger.LogWarning("User {UserId} attempted to update shipping configuration for store {StoreId} without authorization", userId, request.StoreId);
                    throw new UnauthorizedAccessException($"User is not authorized to update shipping configuration for store {request.StoreId}");
                }

                // Validate business rules OUTSIDE retry (no need to revalidate on concurrency conflicts)
                _helper.ValidateConfigurationBusinessRules(request);

                // Execute with retry logic for optimistic concurrency
                return await _concurrencyHelper.ExecuteWithRetryAsync(async () =>
                {
                    // Get fresh configuration on each retry attempt
                    var existingConfiguration = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(request.StoreId, cancellationToken);
                    if (existingConfiguration == null)
                    {
                        throw new InvalidOperationException($"No shipping configuration found for store {request.StoreId}");
                    }

                    // Use Unit of Work transaction INSIDE retry to ensure atomic operation
                    // Configuration and governorates are updated together or not at all
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Update configuration
                        existingConfiguration.DefaultShippingZone = request.DefaultShippingZone.ToString();
                        existingConfiguration.OffersSameDayDelivery = request.OffersSameDayDelivery;
                        existingConfiguration.OffersStandardDelivery = request.OffersStandardDelivery;
                        existingConfiguration.SameDayCutoffHour = request.SameDayCutoffHour;
                        existingConfiguration.ShippingNotes = request.ShippingNotes;
                        existingConfiguration.SameDayDeliveryFee = request.SameDayDeliveryFee;
                        existingConfiguration.StandardDeliveryFee = request.StandardDeliveryFee;
                        existingConfiguration.NationalDeliveryFee = request.NationalDeliveryFee;

                        var updatedConfiguration = await _unitOfWork.StoreShippingConfigurations.UpdateAsync(existingConfiguration, cancellationToken);

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

                        await _unitOfWork.StoreGovernorateSupports.ReplaceStoreGovernorates(request.StoreId, newGovernorateRecords, cancellationToken);

                        // Commit transaction - all changes are saved atomically
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        // Get updated governorate data and store for response - parallelize for better performance
                        var storeTask = _unitOfWork.Stores.GetStoreByIdAsync(request.StoreId, cancellationToken);
                        var supportedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetSupportedGovernorates(request.StoreId, cancellationToken);
                        var excludedGovernoratesTask = _unitOfWork.StoreGovernorateSupports.GetExcludedGovernorates(request.StoreId, cancellationToken);

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
                            SupportedGovernorates = _helper.MapGovernorateShippingInfo(supportedGovernorates),
                            ExcludedGovernorates = _helper.MapGovernorateShippingInfo(excludedGovernorates),
                            CreatedAt = updatedConfiguration.CreatedAt,
                            UpdatedAt = updatedConfiguration.UpdatedAt,
                            IsActive = updatedConfiguration.IsActive,
                            RowVersion = updatedConfiguration.RowVersion
                        };
                    }
                    catch
                    {
                        // Rollback transaction on any error - ensures data consistency
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }, "UpdateShippingConfiguration", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shipping configuration for store: {StoreId}", request?.StoreId);
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

                var configuration = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null)
                {
                    _logger.LogWarning("No shipping configuration found for store: {StoreId}", storeId);
                    return false;
                }

                _logger.LogCritical("PERFORMING HARD DELETE - This action is IRREVERSIBLE. StoreId: {StoreId}, DeletedBy: {DeletedBy}, Reason: {Reason}",
                    storeId, deletedBy, reason);

                var result = await _unitOfWork.StoreShippingConfigurations.DeleteAsync(configuration.ConfigurationId, cancellationToken);
                
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

                var configuration = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null || !configuration.OffersSameDayDelivery)
                {
                    return false;
                }

                // Resolve city to governorate using database lookup
                var cities = await _unitOfWork.Cities.SearchByNameAsync(city, cancellationToken);
                var cityEntity = cities.FirstOrDefault(c => c.Name.Equals(city, StringComparison.OrdinalIgnoreCase));

                if (cityEntity == null)
                {
                    _logger.LogDebug("City {City} not found in database, same-day delivery unavailable", city);
                    return false;
                }

                // Check if store supports this governorate
                var isSupported = await _unitOfWork.StoreGovernorateSupports.IsGovernorateSupportedAsync(storeId, cityEntity.GovernorateId, cancellationToken);
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
                // TODO: FUTURE ENHANCEMENT - Implement per-store timezone support (see detailed plan below)
                // Current implementation assumes all stores operate in Egypt timezone (Africa/Cairo).
                //
                // For multi-region support in the future:
                // 1. Add TimeZoneId column (string, 100 chars) to Store table with default "Africa/Cairo"
                // 2. Create EF Core migration: Add-Migration AddStoreTimeZone
                // 3. Update Store entity with: [StringLength(100)] public string TimeZoneId { get; set; } = "Africa/Cairo";
                // 4. Replace hardcoded "Africa/Cairo" below with: store.TimeZoneId
                // 5. Add timezone validation in StoreShippingConfigurationHelper
                // 6. Update API documentation to specify timezone handling
                //
                // Current behavior: All cutoff times are interpreted as Egypt timezone (UTC+2)
                // Uses IANA timezone ID "Africa/Cairo" which works on both Windows and Linux via TimeZoneConverter
                if (configuration.SameDayCutoffHour.HasValue)
                {
                    // Convert UTC time to Egypt timezone for accurate cutoff comparison
                    // TZConvert.GetTimeZoneInfo automatically handles Windows/Linux differences
                    // "Africa/Cairo" → "Egypt Standard Time" on Windows, "Africa/Cairo" on Linux
                    var egyptTimeZone = TZConvert.GetTimeZoneInfo("Africa/Cairo");
                    var currentEgyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

                    if (currentEgyptTime.Hour > configuration.SameDayCutoffHour.Value)
                    {
                        _logger.LogDebug("Same-day delivery cutoff time passed for store: {StoreId}. Current Egypt time hour: {CurrentHour}, Cutoff: {CutoffHour}",
                            storeId, currentEgyptTime.Hour, configuration.SameDayCutoffHour.Value);
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

                var configuration = await _unitOfWork.StoreShippingConfigurations.GetByStoreIdAsync(storeId, cancellationToken);
                if (configuration == null)
                {
                    return 0; // No configuration means no delivery fee
                }

                // Check for same-day delivery availability inline to avoid duplicate database query
                if (configuration.OffersSameDayDelivery)
                {
                    // Resolve city to governorate using database lookup
                    var cities = await _unitOfWork.Cities.SearchByNameAsync(city, cancellationToken);
                    var cityEntity = cities.FirstOrDefault(c => c.Name.Equals(city, StringComparison.OrdinalIgnoreCase));

                    if (cityEntity != null)
                    {
                        // Check if store supports this governorate
                        var isSupported = await _unitOfWork.StoreGovernorateSupports.IsGovernorateSupportedAsync(storeId, cityEntity.GovernorateId, cancellationToken);

                        if (isSupported && cityEntity.Governorate.SupportsSameDayDelivery)
                        {
                            // Check cutoff time (Egypt timezone)
                            // Uses same cross-platform timezone logic as IsSameDayDeliveryAvailableAsync
                            // See detailed TODO documentation in IsSameDayDeliveryAvailableAsync method
                            if (!configuration.SameDayCutoffHour.HasValue)
                            {
                                return configuration.SameDayDeliveryFee;
                            }

                            // Convert UTC time to Egypt timezone for accurate cutoff comparison
                            // Uses IANA timezone ID "Africa/Cairo" (cross-platform)
                            var egyptTimeZone = TZConvert.GetTimeZoneInfo("Africa/Cairo");
                            var currentEgyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

                            if (currentEgyptTime.Hour <= configuration.SameDayCutoffHour.Value)
                            {
                                return configuration.SameDayDeliveryFee;
                            }
                        }
                    }
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
