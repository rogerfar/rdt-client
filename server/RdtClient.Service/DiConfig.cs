using System.IO.Abstractions;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;
using RdtClient.Service.Wrappers;

namespace RdtClient.Service;

public static class DiConfig
{
    public const String RD_CLIENT = "RdClient";
    public static readonly String UserAgent = $"rdt-client {Assembly.GetEntryAssembly()?.GetName().Version}";

    public static void RegisterRdtServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IAllDebridNetClientFactory, AllDebridNetClientFactory>();
        services.AddScoped<AllDebridDebridClient>();

        services.AddSingleton<IProcessFactory, ProcessFactory>();
        services.AddSingleton<IFileSystem, FileSystem>();

        services.AddScoped<Authentication>();
        services.AddScoped<IDownloads, Downloads>();
        services.AddScoped<Downloads>();
        services.AddScoped<PremiumizeDebridClient>();
        services.AddScoped<QBittorrent>();
        services.AddScoped<Sabnzbd>();
        services.AddScoped<RemoteService>();
        services.AddScoped<RealDebridDebridClient>();
        services.AddScoped<Settings>();
        services.AddScoped<TorBoxDebridClient>();
        services.AddScoped<Torrents>();
        services.AddScoped<TorrentRunner>();
        services.AddScoped<DebridLinkClient>();

        services.AddSingleton<IDownloadableFileFilter, DownloadableFileFilter>();
        services.AddSingleton<ITrackerListGrabber, TrackerListGrabber>();
        services.AddSingleton<IEnricher, Enricher>();

        services.AddSingleton<IAuthorizationHandler, AuthSettingHandler>();
        services.AddScoped<IAuthorizationHandler, SabnzbdHandler>();

        services.AddHostedService<DiskSpaceMonitor>();
        services.AddHostedService<ProviderUpdater>();
        services.AddHostedService<Startup>();
        services.AddHostedService<TaskRunner>();
        services.AddHostedService<UpdateChecker>();
        services.AddHostedService<WatchFolderChecker>();
        services.AddHostedService<WebsocketsUpdater>();
    }

    public static void RegisterHttpClients(this IServiceCollection services)
    {
        var retryPolicy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                          .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        services.AddHttpClient();

        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            });
        });

        services.AddHttpClient(RD_CLIENT)
                .AddPolicyHandler(retryPolicy);
    }
}
