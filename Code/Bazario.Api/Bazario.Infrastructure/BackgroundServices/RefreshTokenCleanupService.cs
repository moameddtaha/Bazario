using Bazario.Core.ServiceContracts.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Background service that automatically cleans up expired refresh tokens from the database
    /// Runs daily to prevent database bloat and maintain performance
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _startDelay;

        public RefreshTokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Run cleanup daily at 2 AM (low traffic time)
            _interval = TimeSpan.FromHours(24);

            // Wait 1 minute after startup before first cleanup
            _startDelay = TimeSpan.FromMinutes(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupService started. Will run every {Interval} hours.", _interval.TotalHours);

            // Wait before first execution to allow application to fully start
            try
            {
                await Task.Delay(_startDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("RefreshTokenCleanupService cancelled during startup delay");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Starting refresh token cleanup cycle");

                    // Create a new scope for each cleanup cycle
                    // This ensures we get fresh instances of scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var refreshTokenService = scope.ServiceProvider
                        .GetRequiredService<IRefreshTokenService>();

                    // Execute cleanup
                    var deletedCount = await refreshTokenService.CleanupExpiredTokensAsync();

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired refresh tokens", deletedCount);
                    }
                    else
                    {
                        _logger.LogDebug("No expired refresh tokens found during cleanup cycle");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the background service
                    _logger.LogError(ex, "Error during refresh token cleanup cycle. Will retry in {Interval} hours.", _interval.TotalHours);
                }

                // Wait for next cleanup cycle
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("RefreshTokenCleanupService stopping - cancellation requested");
                    break;
                }
            }

            _logger.LogInformation("RefreshTokenCleanupService stopped");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupService is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
