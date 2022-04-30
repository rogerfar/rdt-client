using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Helpers;

public static class SettingsHelper
{
    public static String GetString(this IList<Setting> settings, String key)
    {
        var setting = settings.FirstOrDefault(m => m.SettingId == key);

        if (setting == null)
        {
            throw new Exception($"Setting with key {key} not found");
        }

        return setting.Value;
    }

    public static Int32 GetNumber(this IList<Setting> settings, String key)
    {
        var setting = settings.FirstOrDefault(m => m.SettingId == key);

        if (setting == null)
        {
            throw new Exception($"Setting with key {key} not found");
        }

        return Int32.Parse(setting.Value);
    }
}