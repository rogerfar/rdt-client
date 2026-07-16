using System.Collections.Concurrent;

namespace RdtClient.Service.Services;

/// <summary>
///     Remembers infohashes recently found NOT cached on the debrid provider, so the
///     OnlyDownloadCached gate can answer repeat offers of the same release instantly
///     instead of re-asking the provider on every pass. Download managers such as
///     Sonarr or Radarr re-offer a rejected release on every RSS/search pass; the
///     memory also counts sightings so a permanently-uncached release is visible in
///     the logs. Entries expire after <paramref name="recheckInterval" /> - a hash can
///     become cached later, so a rejection must never be permanent.
/// </summary>
public class UncachedHashMemory(TimeSpan recheckInterval)
{
    private readonly ConcurrentDictionary<String, Entry> _entries = new();

    /// <summary>
    ///     True when the provider should be asked about this hash (unknown hash, or the
    ///     re-check interval has elapsed since the last provider answer).
    /// </summary>
    public Boolean NeedsCheck(String hash, DateTimeOffset now)
    {
        return !_entries.TryGetValue(Normalize(hash), out var entry) || now - entry.LastChecked >= recheckInterval;
    }

    /// <summary>
    ///     Record a provider answer of "not cached". Returns the total number of times
    ///     this hash has been seen.
    /// </summary>
    public Int32 RecordUncached(String hash, DateTimeOffset now)
    {
        var entry = _entries.AddOrUpdate(Normalize(hash),
                                         _ => new(now, 1),
                                         (_, previous) => new(now, previous.TimesSeen + 1));

        return entry.TimesSeen;
    }

    /// <summary>
    ///     Record a sighting answered from memory, WITHOUT touching the re-check window -
    ///     under a constant re-offer stream the window would otherwise never elapse and a
    ///     later cache hit would never be discovered. Returns the total sighting count.
    /// </summary>
    public Int32 RecordSighting(String hash)
    {
        var entry = _entries.AddOrUpdate(Normalize(hash),
                                         _ => new(DateTimeOffset.MinValue, 1),
                                         (_, previous) => previous with
                                         {
                                             TimesSeen = previous.TimesSeen + 1
                                         });

        return entry.TimesSeen;
    }

    /// <summary>
    ///     The hash turned out to be cached - forget it.
    /// </summary>
    public void RecordCached(String hash)
    {
        _entries.TryRemove(Normalize(hash), out _);
    }

    private static String Normalize(String hash)
    {
        return hash.ToLowerInvariant();
    }

    private sealed record Entry(DateTimeOffset LastChecked, Int32 TimesSeen);
}
