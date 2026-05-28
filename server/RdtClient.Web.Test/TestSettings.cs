using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;

namespace RdtClient.Web.Test;

internal sealed class TestSettings(DbSettings? settings = null) : ISettings
{
    public DbSettings Current { get; } = settings ?? new();

    public String DefaultSavePath
    {
        get
        {
            var downloadPath = Current.DownloadClient.MappedPath.TrimEnd('\\').TrimEnd('/');

            return downloadPath + Path.DirectorySeparatorChar;
        }
    }

    public IList<SettingProperty> GetAll()
    {
        return [];
    }

    public Task Update(IList<SettingProperty> settings)
    {
        return Task.CompletedTask;
    }

    public Task Update(String settingId, Object? value)
    {
        SetValue(Current, settingId.Split(':'), 0, value);

        return Task.CompletedTask;
    }

    private static void SetValue(Object target, IReadOnlyList<String> path, Int32 index, Object? value)
    {
        var property = target.GetType().GetProperty(path[index]) ?? throw new($"Unknown setting {String.Join(":", path)}");

        if (index < path.Count - 1)
        {
            SetValue(property.GetValue(target)!, path, index + 1, value);

            return;
        }

        if (value == null)
        {
            property.SetValue(target, null);

            return;
        }

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var convertedValue = propertyType.IsEnum ? Enum.Parse(propertyType, value.ToString()!) : Convert.ChangeType(value, propertyType);
        property.SetValue(target, convertedValue);
    }
}
