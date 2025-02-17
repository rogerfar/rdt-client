using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Data;

public static class DiConfig
{
    public static void Config(IServiceCollection services, AppSettings appSettings)
    {
        if (String.IsNullOrWhiteSpace(appSettings.Database?.Path))
        {
            throw new("Invalid database path found in appSettings");
        }

        var connectionString = $"Data Source={appSettings.Database.Path}";
        services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<DownloadData>();
        services.AddScoped<SettingData>();
        services.AddScoped<ITorrentData, TorrentData>();
        services.AddScoped<UserData>();
    }
}