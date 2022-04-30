using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RdtClient.Data.Data;
using Serilog;
using Serilog.Events;

namespace RdtClient.Service.Services;

public class Startup : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public Startup(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await dbContext.Seed();

        var logLevelSettingDb = await dbContext.Settings.FirstOrDefaultAsync(m => m.SettingId == "LogLevel", cancellationToken);
                    
        var logLevelSetting = "Warning";

        if (logLevelSettingDb != null)
        {
            logLevelSetting = logLevelSettingDb.Value;
        }

        if (!Enum.TryParse<LogEventLevel>(logLevelSetting, out var logLevel))
        {
            logLevel = LogEventLevel.Warning;
        }

        Settings.LoggingLevelSwitch.MinimumLevel = logLevel;

        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        Log.Warning($"Starting host on version {version}");

        var settings = scope.ServiceProvider.GetRequiredService<Settings>();

        await settings.ResetCache();
            
        await settings.Clean();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}