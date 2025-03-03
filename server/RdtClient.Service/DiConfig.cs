using System.IO.Abstractions;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using RdtClient.Service.Services.TorrentClients;
using RdtClient.Service.Wrappers;

namespace RdtClient.Service;

public static class DiConfig
{
    public const String RD_CLIENT = "RdClient";

    public static void RegisterRdtServices(this IServiceCollection services)
    {
        services.AddSingleton<IAllDebridNetClientFactory, AllDebridNetClientFactory>();
        services.AddScoped<AllDebridTorrentClient>();

        services.AddSingleton<IProcessFactory, ProcessFactory>();
        services.AddSingleton<IFileSystem, FileSystem>();

        services.AddScoped<Authentication>();
        services.AddScoped<IDownloads, Downloads>();
        services.AddScoped<Downloads>();
        services.AddScoped<PremiumizeTorrentClient>();
        services.AddScoped<QBittorrent>();
        services.AddScoped<RemoteService>();
        services.AddScoped<RealDebridTorrentClient>();
        services.AddScoped<Settings>();
        services.AddScoped<TorBoxTorrentClient>();
        services.AddScoped<Torrents>();
        services.AddScoped<TorrentRunner>();
        services.AddScoped<DebridLinkClient>();

        services.AddSingleton<IDownloadableFileFilter, DownloadableFileFilter>();

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

        var retryPolicy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                          .WaitAndRetryAsync(retryCount: 5, sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        services.AddHttpClient(RD_CLIENT)
                .AddPolicyHandler(retryPolicy);
    }
}
