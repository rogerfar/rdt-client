using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Service.Services;

namespace RdtClient.Service.BackgroundServices;

public class ProviderUpdater : BackgroundService
{
    private readonly ILogger<TaskRunner> _logger;
    private readonly IServiceProvider _serviceProvider;

    private static DateTime _nextUpdate = DateTime.UtcNow;
    
    public ProviderUpdater(ILogger<TaskRunner> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = _serviceProvider.CreateScope();
        var torrentService = scope.ServiceProvider.GetRequiredService<Torrents>();
            
        _logger.LogInformation("ProviderUpdater started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var torrents = await torrentService.Get();
                
                if (_nextUpdate < DateTime.UtcNow && ((torrents.Count > 0 && !Settings.Get.Provider.AutoImport) || Settings.Get.Provider.AutoImport))
                {
                    _logger.LogDebug($"Updating torrent info from Real-Debrid");
                    
                    var updateTime = Settings.Get.Provider.CheckInterval * 3;

                    if (updateTime < 30)
                    {
                        updateTime = 30;
                    }

                    if (RdtHub.HasConnections)
                    {
                        updateTime = Settings.Get.Provider.CheckInterval;

                        if (updateTime < 5)
                        {
                            updateTime = 5;
                        }
                    }

                    _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);

                    await torrentService.UpdateRdData();

                    _logger.LogDebug($"Finished updating torrent info from Real-Debrid, next update in {updateTime} seconds");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred in ProviderUpdater: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("ProviderUpdater stopped.");
    }
}