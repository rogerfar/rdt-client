using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
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

    public static async Task Clean()
    {
        try
        {
            var tempPath = Get.DownloadClient.TempPath;

            if (!String.IsNullOrWhiteSpace(tempPath))
            {
                var files = Directory.GetFiles(tempPath, "*.dsc", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    await FileHelper.Delete(file);
                }
            }
        }
        catch
        {
            // ignored
        }
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