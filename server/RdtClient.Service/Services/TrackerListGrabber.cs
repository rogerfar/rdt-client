using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace RdtClient.Service.Services;

public class TrackerListGrabber(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<TrackerListGrabber> logger) : ITrackerListGrabber
{
    private const String CacheKey = "TrackerList";

    private Int32? _lastExpirationMinutes;

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task<String[]> GetTrackers()
    {
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

        await Semaphore.WaitAsync();

        try
        {
            logger.LogDebug("Tracker cache miss or cache disabled. Fetching tracker list.");
            var trackerUrlList = Settings.Get.General.TrackerEnrichmentList;

            if (String.IsNullOrWhiteSpace(trackerUrlList))
            {
                return [];
            }

            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(trackerUrlList);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            var trackers = result
                           .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .ToArray();

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to fetch tracker list.");

            throw new InvalidOperationException("Unable to fetch tracker list for enrichment.", ex);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}