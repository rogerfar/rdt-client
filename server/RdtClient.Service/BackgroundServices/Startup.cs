using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Data;
using RdtClient.Service.Services;


namespace RdtClient.Service.BackgroundServices;

public class Startup(IServiceProvider serviceProvider) : IHostedService
{
    public static Boolean Ready { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();

        logger.LogWarning("Starting host on version {version}", version);

        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        // Configure SQLite for better concurrency and performance
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA cache_size=-64000;", cancellationToken);

        var settings = scope.ServiceProvider.GetRequiredService<Settings>();
        await settings.Seed();
        await settings.ResetCache();

        Ready = true;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}