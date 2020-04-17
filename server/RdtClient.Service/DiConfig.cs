using Microsoft.Extensions.DependencyInjection;
using RdtClient.Service.Services;

namespace RdtClient.Service
{
    public static class DiConfig
    {
        public static void Config(IServiceCollection services)
        {
            services.AddScoped<IAuthentication, Authentication>();
            services.AddScoped<IDownloads, Downloads>();
            services.AddScoped<IQBittorrent, QBittorrent>();
            services.AddScoped<ISettings, Settings>();
            services.AddScoped<ITorrents, Torrents>();
        }
    }
}