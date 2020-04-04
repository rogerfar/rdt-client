using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Data;

namespace RdtClient.Data
{
    public static class DiConfig
    {
        public static void Config(IServiceCollection services)
        {
            services.AddScoped<IDownloadData, DownloadData>();
            services.AddScoped<ISettingData, SettingData>();
            services.AddScoped<ITorrentData, TorrentData>();
        }
    }
}
