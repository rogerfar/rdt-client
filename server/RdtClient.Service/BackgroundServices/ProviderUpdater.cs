using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Service.Services;

namespace RdtClient.Service.BackgroundServices;

public class ProviderUpdater(ILogger<ProviderUpdater> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private static DateTime _nextUpdate = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = serviceProvider.CreateScope();
        var torrentService = scope.ServiceProvider.GetRequiredService<Torrents>();
            
        logger.LogInformation("ProviderUpdater started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var torrents = await torrentService.Get();

                if (_nextUpdate < DateTime.UtcNow && (Settings.Get.Provider.AutoImport || torrents.Any(t => t.RdStatus != TorrentStatus.Finished)))
                {
                    logger.LogDebug($"Updating torrent info from debrid provider");
                    
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

                    logger.LogDebug("Finished updating torrent info from debrid provider, next update in {updateTime} seconds", updateTime);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred in ProviderUpdater: {ex.Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        logger.LogInformation("ProviderUpdater stopped.");
    }
}