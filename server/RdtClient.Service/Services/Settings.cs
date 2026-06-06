using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Internal;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace RdtClient.Service.Services;

public interface ISettings
{
    DbSettings Current { get; }

    String DefaultSavePath { get; }

    IList<SettingProperty> GetAll();

    Task Update(IList<SettingProperty> settings);

    Task Update(String settingId, Object? value);
}

public class Settings(IServiceScopeFactory serviceScopeFactory) : ISettings
{
    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new(LogEventLevel.Debug);

    public static DbSettings Get => SettingData.Get;

    public DbSettings Current => Get;

    public static String AppDefaultSavePath => GetDefaultSavePath(Get);

    public String DefaultSavePath => GetDefaultSavePath(Current);

    public IList<SettingProperty> GetAll()
    {
        return SettingData.GetAll();
    }

    private static String GetDefaultSavePath(DbSettings settings)
    {
        var downloadPath = settings.DownloadClient.MappedPath;

        if (String.IsNullOrWhiteSpace(downloadPath))
        {
            downloadPath = settings.DownloadClient.DownloadPath;
        }

        downloadPath = downloadPath.TrimEnd('\\')
                                   .TrimEnd('/');

        downloadPath += Path.DirectorySeparatorChar;

        return downloadPath;
    }

    public async Task Update(IList<SettingProperty> settings)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var settingData = scope.ServiceProvider.GetRequiredService<SettingData>();

        await settingData.Update(settings);
    }

    public async Task Update(String settingId, Object? value)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var settingData = scope.ServiceProvider.GetRequiredService<SettingData>();

        await settingData.Update(settingId, value);
    }

    public async Task Seed()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var settingData = scope.ServiceProvider.GetRequiredService<SettingData>();

        await settingData.Seed();
    }

    public async Task ResetCache()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var settingData = scope.ServiceProvider.GetRequiredService<SettingData>();

        await settingData.ResetCache();

        LoggingLevelSwitch.MinimumLevel = Get.General.LogLevel switch
        {
            LogLevel.Verbose => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Warning
        };
    }
}
