using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Service.Services;

namespace RdtClient.Service.BackgroundServices;

public class TaskRunner(ILogger<TaskRunner> logger, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = serviceProvider.CreateScope();
        var torrentRunner = scope.ServiceProvider.GetRequiredService<TorrentRunner>();
            
        logger.LogInformation("TaskRunner started.");

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
                    try
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = await entry.GetDatabaseValuesAsync(stoppingToken);

                        logger.LogWarning("DbUpdateConcurrencyException occurred:");
                        logger.LogWarning("Proposed Values:");
                        logger.LogWarning(JsonSerializer.Serialize(proposedValues));
                        logger.LogWarning("Database Values:");
                        logger.LogWarning(JsonSerializer.Serialize(databaseValues));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error occurred in TaskRunner: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        logger.LogInformation("TaskRunner stopped.");
    }
}