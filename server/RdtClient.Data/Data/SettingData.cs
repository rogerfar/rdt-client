using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Data.Data;

public class SettingData(DataContext dataContext, ILogger<SettingData> logger)
{
    public static DbSettings Get { get; } = new();

    public static IList<SettingProperty> GetAll()
    {
        return GetSettings(Get, null);
    }

    public async Task Update(IList<SettingProperty> settings)
    {
        var dbSettings = await dataContext.Settings.ToListAsync();

        foreach (var dbSetting in dbSettings)
        {
            var setting = settings.FirstOrDefault(m => m.Key == dbSetting.SettingId);

            if (setting != null)
            {
                dbSetting.Value = setting.Value?.ToString();
            }
        }

        await dataContext.SaveChangesAsync();

        await ResetCache();
    }

    public async Task Update(String settingId, Object? value)
    {
        var dbSetting = await dataContext.Settings.FirstOrDefaultAsync(m => m.SettingId == settingId);

        if (dbSetting == null)
        {
            return;
        }

        dbSetting.Value = value?.ToString();

        await dataContext.SaveChangesAsync();

        await ResetCache();
    }

    public async Task ResetCache()
    {
        var settings = await dataContext.Settings.AsNoTracking().ToListAsync();

        if (settings.Count == 0)
        {
            throw new("No settings found, please restart");
        }

        SetSettings(settings, Get, null);
    }

    public async Task Seed()
    {
        var dbSettings = await dataContext.Settings.AsNoTracking().ToListAsync();

        var expectedSettings = GetSettings(Get, null).Where(m => m.Type != "Object").Select(m => new Setting
        {
            SettingId = m.Key,
            Value = m.Value?.ToString()
        }).ToList();

        var newSettings = expectedSettings.Where(m => dbSettings.All(p => p.SettingId != m.SettingId)).ToList();

        if (newSettings.Count > 0)
        {
            await dataContext.Settings.AddRangeAsync(newSettings);
            await dataContext.SaveChangesAsync();
        }

        var oldSettings = dbSettings.Where(m => expectedSettings.All(p => p.SettingId != m.SettingId)).ToList();

        if (oldSettings.Count > 0)
        {
            dataContext.Settings.RemoveRange(oldSettings);
            await dataContext.SaveChangesAsync();
        }
    }

    private static List<SettingProperty> GetSettings(Object defaultSetting, String? parent)
    {
        var result = new List<SettingProperty>();

        var properties = defaultSetting.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var displayName = Attribute.GetCustomAttribute(property, typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            var description = Attribute.GetCustomAttribute(property, typeof(DescriptionAttribute)) as DescriptionAttribute;
            var propertyName = property.Name;

            if (parent != null)
            {
                propertyName = $"{parent}:{propertyName}";
            }

            var settingProperty = new SettingProperty
            {
                Key = propertyName,
                DisplayName = displayName?.DisplayName,
                Description = description?.Description,
                Type = property.PropertyType.Name
            };

            if (property.PropertyType.IsEnum ||
                property.PropertyType.IsValueType ||
                property.PropertyType == typeof(String))
            {
                settingProperty.Value = property.GetValue(defaultSetting);

                if (property.PropertyType.IsEnum)
                {
                    settingProperty.Type = "Enum";
                    settingProperty.EnumValues = [];

                    foreach (var e in Enum.GetValues(property.PropertyType).Cast<Enum>())
                    {
                        var enumMember = property.PropertyType.GetMember(e.ToString()).First();
                        var enumDescriptionAttribute = enumMember.GetCustomAttribute<DescriptionAttribute>();
                        var enumName = enumDescriptionAttribute?.Description ?? Enum.GetName(property.PropertyType, e) ?? "Unknown value";
                        settingProperty.EnumValues.Add((Int32)(Object)e, enumName);
                    }
                }

                result.Add(settingProperty);
            }
            else
            {
                settingProperty.Type = "Object";
                result.Add(settingProperty);

                var childResults = GetSettings(property.GetValue(defaultSetting)!, propertyName);
                result.AddRange(childResults);
            }
        }

        return result;
    }

    private void SetSettings(IList<Setting> settings, Object defaultSetting, String? parent)
    {
        var properties = defaultSetting.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyName = property.Name;

            if (parent != null)
            {
                propertyName = $"{parent}:{propertyName}";
            }

            if (property.PropertyType.IsEnum ||
                property.PropertyType.IsValueType ||
                property.PropertyType == typeof(String))
            {
                var setting = settings.FirstOrDefault(m => m.SettingId == propertyName);

                if (setting != null)
                {
                    if (property.PropertyType.IsEnum)
                    {
                        try
                        {
                            var newValue = Enum.Parse(property.PropertyType, setting.Value ?? "0");
                            property.SetValue(defaultSetting, newValue);
                        }
                        catch
                        {
                            logger.LogWarning("Invalid value for setting {propertyName}: {setting.Value}", propertyName, setting.Value);

                            var defaultValue = property.GetValue(defaultSetting);
                            property.SetValue(defaultSetting, defaultValue);
                        }
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(property.PropertyType);

                        if (setting.Value == null)
                        {
                            property.SetValue(defaultSetting, null);
                        }
                        else if (converter.IsValid(setting.Value))
                        {
                            var newValue = converter.ConvertFrom(setting.Value);
                            property.SetValue(defaultSetting, newValue);
                        }
                        else
                        {
                            logger.LogWarning("Invalid value for setting {propertyName}: {setting.Value}", propertyName, setting.Value);

                            var defaultValue = property.GetValue(defaultSetting);
                            property.SetValue(defaultSetting, defaultValue);
                        }
                    }
                }
            }
            else
            {
                SetSettings(settings, property.GetValue(defaultSetting)!, propertyName);
            }
        }
    }
}