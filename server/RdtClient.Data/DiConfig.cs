using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Data;

public static class DiConfig
{
    public static void Config(IServiceCollection services, AppSettings appSettings)
    {
        var connectionString = $"Data Source={appSettings.Database.Path}";
        services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<DownloadData>();
        services.AddScoped<SettingData>();
        services.AddScoped<TorrentData>();
        services.AddScoped<UserData>();
    }
}