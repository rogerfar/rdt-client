using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Service.Services;

namespace RdtClient.Service.BackgroundServices;

public class WebsocketsUpdater(ILogger<WebsocketsUpdater> logger, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        var scope = serviceProvider.CreateScope();

        var remoteService = scope.ServiceProvider.GetRequiredService<RemoteService>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await remoteService.Update();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error occurred in WebsocketsUpdater: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        logger.LogInformation("WebsocketsUpdater stopped.");
    }
}