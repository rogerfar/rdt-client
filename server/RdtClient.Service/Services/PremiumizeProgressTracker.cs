using System.Collections.Concurrent;

namespace RdtClient.Service.Services;

public class PremiumizeProgressTracker : IPremiumizeProgressTracker
{
    public const Int64 SyntheticSizeBytes = 1_000_000_000;

    public static readonly TimeSpan StallThreshold = TimeSpan.FromMinutes(20);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    internal Func<DateTimeOffset>? UtcNowOverride { get; set; }

    private DateTimeOffset UtcNow => UtcNowOverride?.Invoke() ?? DateTimeOffset.UtcNow;

    public void Update(Guid torrentId, Double progressFraction)
    {
        var now = UtcNow;
        var clampedProgress = Math.Clamp(progressFraction, 0.0, 1.0);

        _entries.AddOrUpdate(
            torrentId,
            _ => CreateEntry(clampedProgress, now),
            (_, existing) => UpdateEntry(existing, clampedProgress, now));
    }

    public Boolean IsStalled(Guid torrentId)
    {
        if (!_entries.TryGetValue(torrentId, out var entry))
        {
            return false;
        }

        return entry.IsStalled;
    }

    public Int64? GetSpeedBytesPerSec(Guid torrentId)
    {
        if (!_entries.TryGetValue(torrentId, out var entry))
        {
            return null;
        }

        return entry.LastSpeedBytesPerSec;
    }

    public Double? GetProgressPerMin(Guid torrentId)
    {
        if (!_entries.TryGetValue(torrentId, out var entry))
        {
            return null;
        }

        return entry.LastProgressPerMin;
    }

    public void Remove(Guid torrentId)
    {
        _entries.TryRemove(torrentId, out _);
    }

    private static Entry CreateEntry(Double progress, DateTimeOffset now)
    {
        return new()
        {
            LastProgress = progress,
            LastProgressAt = now,
            LastUpdatedAt = now,
            IsStalled = false
        };
    }

    private static Entry UpdateEntry(Entry existing, Double progress, DateTimeOffset now)
    {
        var elapsed = now - existing.LastUpdatedAt;
        var progressDelta = progress - existing.LastProgress;

        if (progressDelta > 0.0 && elapsed.TotalSeconds > 0)
        {
            var speedBytesPerSec = (Int64)((progressDelta * SyntheticSizeBytes) / elapsed.TotalSeconds);
            var progressPerMin = progressDelta * 100.0 / elapsed.TotalMinutes;

            return existing with
            {
                LastProgress = progress,
                LastProgressAt = now,
                LastUpdatedAt = now,
                LastSpeedBytesPerSec = Math.Max(speedBytesPerSec, 0),
                LastProgressPerMin = progressPerMin,
                IsStalled = false
            };
        }

        if (progress > existing.LastProgress)
        {
            return existing with
            {
                LastProgress = progress,
                LastProgressAt = now,
                LastUpdatedAt = now,
                IsStalled = false
            };
        }

        var stalled = now - existing.LastProgressAt >= StallThreshold;

        return existing with
        {
            LastProgress = progress,
            LastUpdatedAt = now,
            IsStalled = stalled
        };
    }

    private sealed record Entry
    {
        public required Double LastProgress { get; init; }

        public required DateTimeOffset LastProgressAt { get; init; }

        public required DateTimeOffset LastUpdatedAt { get; init; }

        public Int64 LastSpeedBytesPerSec { get; init; }

        public Double? LastProgressPerMin { get; init; }

        public Boolean IsStalled { get; init; }
    }
}
