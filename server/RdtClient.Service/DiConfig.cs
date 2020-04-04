using Microsoft.Extensions.DependencyInjection;
using RdtClient.Service.Services;

namespace RdtClient.Service
{
    public static class DiConfig
    {
        public static void Config(IServiceCollection services)
        {
            services.AddScoped<IDownloads, Downloads>();
            services.AddScoped<IScheduler, Scheduler>();
            services.AddScoped<ISettings, Settings>();
            services.AddScoped<ITorrents, Torrents>();
        }
    }
}