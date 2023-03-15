using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = await entry.GetDatabaseValuesAsync(stoppingToken);
                    
                    _logger.LogWarning("DbUpdateConcurrencyException occurred:");
                    _logger.LogWarning("Proposed Values:");
                    _logger.LogWarning(JsonSerializer.Serialize(proposedValues));
                    _logger.LogWarning("Database Values:");
                    _logger.LogWarning(JsonSerializer.Serialize(databaseValues));
                }
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