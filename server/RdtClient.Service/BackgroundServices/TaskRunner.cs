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

        logger.LogInformation("TaskRunner started.");

        using (var startupScope = serviceProvider.CreateScope())
        {
            var startupRunner = startupScope.ServiceProvider.GetRequiredService<TorrentRunner>();
            await startupRunner.Initialize();
        }

        var consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var torrentRunner = scope.ServiceProvider.GetRequiredService<TorrentRunner>();

            try
            {
                await torrentRunner.Tick();

                consecutiveFailures = 0;
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
                consecutiveFailures++;

                logger.LogError(ex, $"Unexpected error occurred in TaskRunner: {ex.Message}");
            }

            var delay = GetTickDelay(consecutiveFailures);

            if (delay > TimeSpan.FromSeconds(1))
            {
                logger.LogWarning("TorrentRunner tick failed {consecutiveFailures} times in a row, backing off for {delay}", consecutiveFailures, delay);
            }

            await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("TaskRunner stopped.");
    }

    private static TimeSpan GetTickDelay(Int32 consecutiveFailures)
    {
        if (consecutiveFailures <= 1)
        {
            return TimeSpan.FromSeconds(1);
        }

        var seconds = Math.Min(300d, 5d * Math.Pow(2, Math.Min(consecutiveFailures - 2, 6)));

        return TimeSpan.FromSeconds(seconds);
    }
}
