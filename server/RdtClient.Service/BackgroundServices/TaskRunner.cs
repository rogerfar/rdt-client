using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Service.Services;

namespace RdtClient.Service.BackgroundServices;

public class TaskRunner : BackgroundService
{
    private readonly ILogger<TaskRunner> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TaskRunner(ILogger<TaskRunner> logger, IServiceProvider serviceProvider)
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
        var torrentRunner = scope.ServiceProvider.GetRequiredService<TorrentRunner>();
            
        _logger.LogInformation("TaskRunner started.");

        await torrentRunner.Initialize();
            
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await torrentRunner.Tick();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred in TorrentDownloadManager.Tick: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("TaskRunner stopped.");
    }
}