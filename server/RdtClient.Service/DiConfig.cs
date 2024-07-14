using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Handlers;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using RdtClient.Service.Services.TorrentClients;

namespace RdtClient.Service;

public static class DiConfig
{
    public const String RD_CLIENT = "RdClient";
    
    public static void RegisterRdtServices(this IServiceCollection services)
    {
        services.AddScoped<AllDebridTorrentClient>();
        services.AddScoped<Authentication>();
        services.AddScoped<Downloads>();
        services.AddScoped<PremiumizeTorrentClient>();
        services.AddScoped<QBittorrent>();
        services.AddScoped<RemoteService>();
        services.AddScoped<RealDebridTorrentClient>();
        services.AddScoped<Settings>();
        services.AddScoped<Torrents>();
        services.AddScoped<TorrentRunner>();

        services.AddSingleton<IAuthorizationHandler, AuthSettingHandler>();
            
        services.AddHostedService<ProviderUpdater>();
        services.AddHostedService<Startup>();
        services.AddHostedService<TaskRunner>();
        services.AddHostedService<UpdateChecker>();
        services.AddHostedService<WatchFolderChecker>();
    }

    public static void RegisterHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient();
        
        services.AddHttpClient(RD_CLIENT)
               .AddHttpMessageHandler(sp => new RateLimitingHandler(sp.GetRequiredService<ILogger<RateLimitingHandler>>(), 60, TimeSpan.FromSeconds(60)))
               .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }
}