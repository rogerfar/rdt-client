using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Data;

namespace RdtClient.Data
{
    public static class DiConfig
    {
        public static void Config(IServiceCollection services)
        {
            services.AddScoped<DownloadData>();
            services.AddScoped<SettingData>();
            services.AddScoped<TorrentData>();
            services.AddScoped<UserData>();
        }
    }
}
