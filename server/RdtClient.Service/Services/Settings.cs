using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Internal;
using Serilog.Core;
using Serilog.Events;

namespace RdtClient.Service.Services;

public class Settings(SettingData settingData)
{
    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new(LogEventLevel.Debug);

    public static DbSettings Get => SettingData.Get;

    public static String AppDefaultSavePath
    {
        get
        {
            var downloadPath = Get.DownloadClient.MappedPath;

            downloadPath = downloadPath.TrimEnd('\\')
                                       .TrimEnd('/');

            downloadPath += Path.DirectorySeparatorChar;

            return downloadPath;
        }
    }

    public async Task Update(IList<SettingProperty> settings)
    {
        await settingData.Update(settings);
    }

    public async Task Update(String settingId, Object? value)
    {
        await settingData.Update(settingId, value);
    }

    public async Task Seed()
    {
        await settingData.Seed();
    }

    public async Task ResetCache()
    {
        await settingData.ResetCache();

        LoggingLevelSwitch.MinimumLevel = Settings.Get.General.LogLevel switch
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