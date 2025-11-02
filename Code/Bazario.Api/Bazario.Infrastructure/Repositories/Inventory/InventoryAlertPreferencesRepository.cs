using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Inventory;
using Bazario.Core.Domain.RepositoryContracts.Inventory;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories.Inventory
{
    /// <summary>
    /// Repository implementation for managing inventory alert preferences with database persistence
    /// </summary>
    public class InventoryAlertPreferencesRepository : IInventoryAlertPreferencesRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryAlertPreferencesRepository> _logger;

        public InventoryAlertPreferencesRepository(
            ApplicationDbContext context,
            ILogger<InventoryAlertPreferencesRepository> _logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        public async Task<InventoryAlertPreferences?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving alert preferences for store {StoreId}", storeId);

            try
            {
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to get preferences with empty store ID");
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                var preferences = await _context.InventoryAlertPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.StoreId == storeId, cancellationToken);

                if (preferences != null)
                {
                    _logger.LogDebug("Found alert preferences for store {StoreId}", storeId);
                }
                else
                {
                    _logger.LogDebug("No alert preferences found for store {StoreId}", storeId);
                }

                return preferences;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert preferences for store {StoreId}", storeId);
                throw new InvalidOperationException($"Error retrieving alert preferences: {ex.Message}", ex);
            }
        }

        public async Task<InventoryAlertPreferences> UpsertAsync(InventoryAlertPreferences preferences, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Upserting alert preferences for store {StoreId}", preferences?.StoreId);

            try
            {
                if (preferences == null)
                {
                    _logger.LogWarning("Attempted to upsert null preferences");
                    throw new ArgumentNullException(nameof(preferences));
                }

                if (preferences.StoreId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to upsert preferences with empty store ID");
                    throw new ArgumentException("Store ID cannot be empty", nameof(preferences));
                }

                // Check if preferences already exist
                var existing = await _context.InventoryAlertPreferences
                    .FirstOrDefaultAsync(p => p.StoreId == preferences.StoreId, cancellationToken);

                if (existing != null)
                {
                    // Update existing preferences
                    _logger.LogDebug("Updating existing alert preferences for store {StoreId}", preferences.StoreId);

                    existing.AlertEmail = preferences.AlertEmail;
                    existing.EnableLowStockAlerts = preferences.EnableLowStockAlerts;
                    existing.EnableOutOfStockAlerts = preferences.EnableOutOfStockAlerts;
                    existing.EnableRestockRecommendations = preferences.EnableRestockRecommendations;
                    existing.EnableDeadStockAlerts = preferences.EnableDeadStockAlerts;
                    existing.DefaultLowStockThreshold = preferences.DefaultLowStockThreshold;
                    existing.DeadStockDays = preferences.DeadStockDays;
                    existing.SendDailySummary = preferences.SendDailySummary;
                    existing.SendWeeklySummary = preferences.SendWeeklySummary;
                    existing.UpdatedAt = DateTime.UtcNow;

                    _context.InventoryAlertPreferences.Update(existing);
                    _logger.LogInformation("Updated alert preferences for store {StoreId}", preferences.StoreId);

                    return existing;
                }
                else
                {
                    // Insert new preferences
                    _logger.LogDebug("Creating new alert preferences for store {StoreId}", preferences.StoreId);

                    preferences.CreatedAt = DateTime.UtcNow;
                    preferences.UpdatedAt = DateTime.UtcNow;

                    await _context.InventoryAlertPreferences.AddAsync(preferences, cancellationToken);
                    _logger.LogInformation("Created new alert preferences for store {StoreId}", preferences.StoreId);

                    return preferences;
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting alert preferences for store {StoreId}", preferences?.StoreId);
                throw new InvalidOperationException($"Error upserting alert preferences: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting alert preferences for store {StoreId}", storeId);

            try
            {
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete preferences with empty store ID");
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                var preferences = await _context.InventoryAlertPreferences
                    .FirstOrDefaultAsync(p => p.StoreId == storeId, cancellationToken);

                if (preferences == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent preferences for store {StoreId}", storeId);
                    return false;
                }

                _context.InventoryAlertPreferences.Remove(preferences);
                _logger.LogInformation("Deleted alert preferences for store {StoreId}", storeId);

                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert preferences for store {StoreId}", storeId);
                throw new InvalidOperationException($"Error deleting alert preferences: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if alert preferences exist for store {StoreId}", storeId);

            try
            {
                if (storeId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to check existence with empty store ID");
                    throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
                }

                var exists = await _context.InventoryAlertPreferences
                    .AnyAsync(p => p.StoreId == storeId, cancellationToken);

                _logger.LogDebug("Alert preferences {ExistStatus} for store {StoreId}",
                    exists ? "exist" : "do not exist", storeId);

                return exists;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if alert preferences exist for store {StoreId}", storeId);
                throw new InvalidOperationException($"Error checking alert preferences existence: {ex.Message}", ex);
            }
        }
    }
}
