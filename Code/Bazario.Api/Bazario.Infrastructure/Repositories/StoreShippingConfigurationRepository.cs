using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for store shipping configuration operations
    /// </summary>
    public class StoreShippingConfigurationRepository : IStoreShippingConfigurationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StoreShippingConfigurationRepository> _logger;

        public StoreShippingConfigurationRepository(ApplicationDbContext context, ILogger<StoreShippingConfigurationRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreShippingConfiguration?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting shipping configuration for store: {StoreId}", storeId);

            try
            {
                return await _context.StoreShippingConfigurations
                    .Include(sc => sc.Store)
                    .FirstOrDefaultAsync(sc => sc.StoreId == storeId && sc.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get shipping configuration for store: {StoreId}", storeId);
                throw;
            }
        }

        public async Task<StoreShippingConfiguration> CreateAsync(StoreShippingConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Creating shipping configuration for store: {StoreId}", configuration.StoreId);

            try
            {
                configuration.ConfigurationId = Guid.NewGuid();
                configuration.CreatedAt = DateTime.UtcNow;
                configuration.UpdatedAt = DateTime.UtcNow;

                _context.StoreShippingConfigurations.Add(configuration);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully created shipping configuration: {ConfigurationId} for store: {StoreId}", 
                    configuration.ConfigurationId, configuration.StoreId);

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shipping configuration for store: {StoreId}", configuration.StoreId);
                throw;
            }
        }

        public async Task<StoreShippingConfiguration> UpdateAsync(StoreShippingConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating shipping configuration: {ConfigurationId} for store: {StoreId}", 
                configuration.ConfigurationId, configuration.StoreId);

            try
            {
                // Get the existing entity from the database
                var existingConfiguration = await _context.StoreShippingConfigurations
                    .FirstOrDefaultAsync(sc => sc.ConfigurationId == configuration.ConfigurationId, cancellationToken);

                if (existingConfiguration == null)
                {
                    throw new InvalidOperationException($"Shipping configuration with ID {configuration.ConfigurationId} not found");
                }

                // Update only the specific properties that should be changed
                existingConfiguration.DefaultShippingZone = configuration.DefaultShippingZone;
                existingConfiguration.OffersSameDayDelivery = configuration.OffersSameDayDelivery;
                existingConfiguration.OffersStandardDelivery = configuration.OffersStandardDelivery;
                existingConfiguration.SameDayCutoffHour = configuration.SameDayCutoffHour;
                existingConfiguration.ShippingNotes = configuration.ShippingNotes;
                existingConfiguration.SameDayDeliveryFee = configuration.SameDayDeliveryFee;
                existingConfiguration.StandardDeliveryFee = configuration.StandardDeliveryFee;
                existingConfiguration.SupportedCities = configuration.SupportedCities;
                existingConfiguration.ExcludedCities = configuration.ExcludedCities;
                existingConfiguration.IsActive = configuration.IsActive;
                existingConfiguration.UpdatedAt = DateTime.UtcNow;

                // Note: We don't update sensitive/immutable properties like:
                // - ConfigurationId (primary key)
                // - StoreId (foreign key)
                // - CreatedAt (audit field)

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated shipping configuration: {ConfigurationId} for store: {StoreId}", 
                    configuration.ConfigurationId, configuration.StoreId);

                return existingConfiguration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shipping configuration: {ConfigurationId} for store: {StoreId}", 
                    configuration.ConfigurationId, configuration.StoreId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid configurationId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Deleting shipping configuration: {ConfigurationId}", configurationId);

            try
            {
                var configuration = await _context.StoreShippingConfigurations
                    .FirstOrDefaultAsync(sc => sc.ConfigurationId == configurationId, cancellationToken);

                if (configuration == null)
                {
                    _logger.LogWarning("Shipping configuration not found: {ConfigurationId}", configurationId);
                    return false;
                }

                configuration.IsActive = false;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted shipping configuration: {ConfigurationId}", configurationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shipping configuration: {ConfigurationId}", configurationId);
                throw;
            }
        }

        public async Task<bool> ExistsForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if shipping configuration exists for store: {StoreId}", storeId);

            try
            {
                return await _context.StoreShippingConfigurations
                    .AnyAsync(sc => sc.StoreId == storeId && sc.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if shipping configuration exists for store: {StoreId}", storeId);
                throw;
            }
        }
    }
}
