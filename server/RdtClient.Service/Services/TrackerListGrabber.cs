using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace RdtClient.Service.Services;

public class TrackerListGrabber(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<TrackerListGrabber> logger) : ITrackerListGrabber
{
    private const String CacheKey = "TrackerList";

    private Int32? _lastExpirationMinutes;

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task<String[]> GetTrackers()
    {
        var trackerUrlList = Settings.Get.General.TrackerEnrichmentList;

        if (String.IsNullOrWhiteSpace(trackerUrlList))
        {
            return [];
        }

        if (!Uri.TryCreate(trackerUrlList, UriKind.Absolute, out var trackerUri) ||
            (trackerUri.Scheme != Uri.UriSchemeHttp && trackerUri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("Invalid tracker list URL format: {Url}", trackerUrlList);

            return [];
        }

        var currentExpiration = Settings.Get.General.TrackerEnrichmentCacheExpiration;
        var useCache = currentExpiration > 0;

        if (!useCache)
        {
            memoryCache.Remove(CacheKey);
            _lastExpirationMinutes = null;
        }
        else
        {
            if (_lastExpirationMinutes is not null && currentExpiration != _lastExpirationMinutes)
            {
                logger.LogDebug("Tracker list cache timeout changed, invalidating cache.");
                memoryCache.Remove(CacheKey);
            }

            _lastExpirationMinutes = currentExpiration;

            if (memoryCache.TryGetValue(CacheKey, out String[]? cachedTrackers) && cachedTrackers is { Length: > 0 })
            {
                logger.LogDebug("Using cached tracker list.");

                return cachedTrackers;
            }
        }

        await Semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (useCache)
            {
                if (memoryCache.TryGetValue(CacheKey, out String[]? cachedTrackers) && cachedTrackers is { Length: > 0 })
                {
                    logger.LogDebug("Using cached tracker list (after lock).");

                    return cachedTrackers;
                }
            }

            logger.LogDebug("Tracker cache miss or cache disabled. Fetching tracker list.");

            var trackers = await FetchAndParseTrackersAsync(trackerUri).ConfigureAwait(false);

            if (useCache)
            {
                memoryCache.Set(CacheKey,
                                trackers,
                                new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(currentExpiration)
                                });
            }

            return trackers;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Fetching tracker list was canceled (timeout or cancellation).");

            throw new TaskCanceledException("Fetching tracker list was canceled due to timeout or cancellation.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to fetch tracker list.");

            throw new("Unable to fetch tracker list for enrichment.", ex);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task<String[]> FetchAndParseTrackersAsync(Uri trackerUri)
    {
        logger.LogDebug("Fetching tracker list from URL: {TrackerUrl}", trackerUri);

        var httpClient = httpClientFactory.CreateClient();

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        var currentVersion = version != null && version.LastIndexOf('.') > 0
            ? $"v{version[..version.LastIndexOf('.')]}"
            : "";

        httpClient.DefaultRequestHeaders.UserAgent.Add(new("RdtClient", currentVersion));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = cts.Token;
        var response = await httpClient.GetAsync(trackerUri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

        using (response)
        {
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var reader = new StreamReader(contentStream);
            var result = await reader.ReadToEndAsync(token).ConfigureAwait(false);

            String[] trackers;

            try
            {
                var trackerRejectionCount = 0;

                trackers = result
                           .Split([
                                      "\r\n", "\n"
                                  ],
                                  StringSplitOptions.RemoveEmptyEntries)
                           .Where(line => !String.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
                           .Select(t => t.EndsWith("/") ? t.TrimEnd('/') : t)
                           .Select(t => t.Trim())
                           .Where(t =>
                           {
                               if (!Uri.TryCreate(t, UriKind.Absolute, out var uri))
                               {
                                   logger.LogDebug("Rejected tracker: {TrackerUrl} - Reason: Invalid format or unsupported scheme.", t);
                                   trackerRejectionCount++;
                                   return false;
                               }

                               var isIpv6 = uri.Host.StartsWith('[') && uri.Host.Contains(']');

                               var valid = ((isIpv6 && uri.Host.All(c => Char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '[' || c == ']')) ||
                                            (!isIpv6 && uri.Host.All(c => Char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))) &&
                                           (uri.Scheme == Uri.UriSchemeHttp ||
                                            uri.Scheme == Uri.UriSchemeHttps ||
                                            uri.Scheme.Equals("udp", StringComparison.OrdinalIgnoreCase) ||
                                            uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase)) &&
                                           !t.Contains("..") &&
                                           !t.Contains("\\") &&
                                           !t.Any(Char.IsControl) &&
                                           uri.Host.Length > 0;

                               if (!valid)
                               {
                                   logger.LogDebug("Enrichment tracker rejected: {TrackerUrl} - Reason: Invalid format or unsupported scheme.", t);
                                   trackerRejectionCount++;
                               }

                               return valid;
                           })
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .ToArray();

                logger.LogInformation("{TrackerRejectionCount} trackers were rejected during enrichment.", trackerRejectionCount);
            }
            catch (Exception ex)

            {
                logger.LogError(ex, "Error parsing tracker list response.");

                throw new InvalidOperationException("Failed to parse tracker list response.", ex);
            }

            return trackers;
        }
    }
}
