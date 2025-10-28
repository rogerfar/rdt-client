using System.IO.Abstractions;
using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Polly;
using Polly.Retry;
using Polly.RateLimiting;

using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using RdtClient.Service.Services.TorrentClients;
using RdtClient.Service.Wrappers;

namespace RdtClient.Service;

public static class DiConfig
{
    public const String RD_CLIENT = "RdClient";
    public const String TORBOX_CLIENT = "TorboxClient";
    public const String TORBOX_CLIENT_CREATETORRENT = "TorboxClientCreateTorrent";
    public static readonly String UserAgent = $"rdt-client {Assembly.GetEntryAssembly()?.GetName().Version}";

    private static readonly SlidingWindowRateLimiter TorboxPerSecondLimiter =
        new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 4,
            QueueLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    private static readonly SlidingWindowRateLimiter TorboxCreateTorrentPerMinuteLimiter =
        new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    private static readonly SlidingWindowRateLimiter TorboxCreateTorrentPerHourLimiter =
        new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromHours(1),
            SegmentsPerWindow = 60,
            QueueLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    public static void RegisterRdtServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

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
        services.AddSingleton<ITrackerListGrabber, TrackerListGrabber>();
        services.AddSingleton<IEnricher, Enricher>();

        services.AddSingleton<IAuthorizationHandler, AuthSettingHandler>();

        services.AddHostedService<ProviderUpdater>();
        services.AddHostedService<Startup>();
        services.AddHostedService<TaskRunner>();
        services.AddHostedService<UpdateChecker>();
        services.AddHostedService<WatchFolderChecker>();
        services.AddHostedService<WebsocketsUpdater>();
    }

    public static void RegisterHttpClients(this IServiceCollection services)
    {
        var retryStrategy = new RetryStrategyOptions<HttpResponseMessage>
        {
            // Transient failures to handle (network errors, 5xx, 408, 429)
            ShouldHandle = static args =>
            {
                if (args.Outcome.Exception is HttpRequestException)
                {
                    return ValueTask.FromResult(true);
                }

                if (args.Outcome.Result is HttpResponseMessage r &&
                    (((int)r.StatusCode >= 500) ||
                    r.StatusCode == HttpStatusCode.RequestTimeout ||
                    r.StatusCode == HttpStatusCode.TooManyRequests))
                {
                    return ValueTask.FromResult(true);
                }

                return ValueTask.FromResult(false);
            },

            // Default backoff when Retry-After is not provided
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,

            // Prefer server-provided delay; otherwise use the internal backoff
            DelayGenerator = static args =>
            {
                if (args.Outcome.Result is HttpResponseMessage resp && resp.Headers.RetryAfter is { } ra)
                {
                    if (ra.Delta is TimeSpan delta)
                    {
                        return new ValueTask<TimeSpan?>(delta);
                    }

                    if (ra.Date is DateTimeOffset when)
                    {
                        var delay = when - DateTimeOffset.UtcNow;
                        return new ValueTask<TimeSpan?>(delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
                    }
                }

                // null => use the configured backoff (Delay/BackoffType/UseJitter)
                return new ValueTask<TimeSpan?>((TimeSpan?)null);
            }
        };

        services.AddHttpClient(RD_CLIENT, httpClient =>
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        })
        .AddResilienceHandler("DefaultRetryPolicy", builder =>
        {
            builder.AddRetry(retryStrategy);
        });

        services.AddHttpClient(TORBOX_CLIENT, httpClient =>
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        })
        .AddResilienceHandler("TorBox-Limits", (builder, ctx) =>
        {
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                // Acquire 1 permit; throw RateLimiterRejectedException if unavailable
                RateLimiter = args => TorboxPerSecondLimiter.AcquireAsync(1, args.Context.CancellationToken),

                // Optional notification just before rejection is thrown
                OnRejected = _ => default
            });

            // Keep your existing retry here so transient failures still retry
            builder.AddRetry(retryStrategy);
        });

        services.AddHttpClient(TORBOX_CLIENT_CREATETORRENT, httpClient =>
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        })
        .AddResilienceHandler("TorBox-CreateTorrent-Limits", (builder, ctx) =>
        {
            // First limiter: 10 per minute
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => TorboxCreateTorrentPerMinuteLimiter.AcquireAsync(1, args.Context.CancellationToken),
                OnRejected = _ => default
            });

            // Second limiter: 60 per hour
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => TorboxCreateTorrentPerHourLimiter.AcquireAsync(1, args.Context.CancellationToken),
                OnRejected = _ => default
            });
            builder.AddRetry(retryStrategy);
        });
    }
}
