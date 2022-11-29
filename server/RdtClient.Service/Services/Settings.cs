using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Internal;
using Serilog.Core;
using Serilog.Events;

namespace RdtClient.Service.Services;

public class Settings
{
    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new(LogEventLevel.Debug);

    private readonly SettingData _settingData;

    public Settings(SettingData settingData)
    {
        _settingData = settingData;
    }

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
        await _settingData.Update(settings);
    }

    public async Task Update(String settingId, Object? value)
    {
        await _settingData.Update(settingId, value);
    }

    public async Task Seed()
    {
        await _settingData.Seed();
    }

    public async Task ResetCache()
    {
        await _settingData.ResetCache();

        LoggingLevelSwitch.MinimumLevel = Settings.Get.General.LogLevel switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Warning
        };
    }
}