using System.IO.Abstractions;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;
using RateLimitHeaders.Polly;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Helpers;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;
using RdtClient.Service.Wrappers;

namespace RdtClient.Service;

public static class DiConfig
{
    public const String RD_CLIENT = "RdClient";
    public const String TORBOX_CLIENT = "TorBoxClient";
    public const String TORBOX_CLIENT_SLOW = "TorBoxClientSlow";
    public static readonly String UserAgent = $"rdt-client {Assembly.GetEntryAssembly()?.GetName().Version}";

    public static void RegisterRdtServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IAllDebridNetClientFactory, AllDebridNetClientFactory>();
        services.AddScoped<AllDebridDebridClient>();

        services.AddSingleton<IRateLimitCoordinator, RateLimitCoordinator>();
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
        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            });
        });

        services.AddTransient<RateLimitHandler>();
        
        services.AddHttpClient(RD_CLIENT)
                .AddHttpMessageHandler<RateLimitHandler>()
                .AddResilienceHandler("rd_client_handler", ConfigureResiliencePipeline);

        // This likely works for most providers, but should be verified and then the providers changed
        // to this HTTP client for added resilience.
        services.AddHttpClient(TORBOX_CLIENT)
                .AddResilienceHandler("torbox_client_handler", ConfigureResiliencePipeline);
        
        services.AddHttpClient(TORBOX_CLIENT_SLOW)
                .AddHttpMessageHandler<RateLimitHandler>()
                .AddResilienceHandler("torbox_client_handler_slow", ConfigureResiliencePipeline);
    }

    private static void ConfigureResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            TimeoutGenerator = _ => new(TimeSpan.FromSeconds(Settings.Get.Provider.Timeout))
        });

        builder.AddRateLimitHeaders(options =>
        {
            options.EnableProactiveThrottling = true;
        });

        builder.AddRetry(new()
        {
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException } => PredicateResult.True(),
                { Result.StatusCode: HttpStatusCode.RequestTimeout } => PredicateResult.True(),
                { Result.StatusCode: HttpStatusCode.TooManyRequests } => PredicateResult.True(),
                _ => PredicateResult.False()
            },
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            UseJitter = true,
            DelayGenerator = args =>
            {
                if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests } response)
                {
                    var delay = RateLimitHandler.GetRetryAfterDelay(response);

                    if (delay >= TimeSpan.FromMinutes(10))
                    {
                        return new ValueTask<TimeSpan?>((TimeSpan?)null);
                    }

                    return new(delay);
                }

                return new((TimeSpan?)null);
            }
        });
    }
}
