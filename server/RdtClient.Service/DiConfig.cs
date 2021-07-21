using Microsoft.Extensions.DependencyInjection;
using RdtClient.Service.Services;

namespace RdtClient.Service
{
    public static class DiConfig
    {
        public static void Config(IServiceCollection services)
        {
            services.AddScoped<Authentication>();
            services.AddScoped<Downloads>();
            services.AddScoped<QBittorrent>();
            services.AddScoped<RemoteService>();
            services.AddScoped<Settings>();
            services.AddScoped<Torrents>();
            services.AddScoped<TorrentRunner>();
            
            services.AddHostedService<Startup>();
        }
    }
}