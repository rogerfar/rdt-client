using Microsoft.Extensions.DependencyInjection;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Services;
using RdtClient.Service.Services.TorrentClients;

namespace RdtClient.Service;

public static class DiConfig
{
    public static void Config(IServiceCollection services)
    {
        services.AddScoped<AllDebridTorrentClient>();
        services.AddScoped<Authentication>();
        services.AddScoped<Downloads>();
        services.AddScoped<QBittorrent>();
        services.AddScoped<RemoteService>();
        services.AddScoped<RealDebridTorrentClient>();
        services.AddScoped<Settings>();
        services.AddScoped<Torrents>();
        services.AddScoped<TorrentRunner>();
            
        services.AddHostedService<ProviderUpdater>();
        services.AddHostedService<Startup>();
        services.AddHostedService<TaskRunner>();
        services.AddHostedService<UpdateChecker>();
    }
}