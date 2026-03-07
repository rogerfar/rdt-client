using System.Collections.Concurrent;

namespace RdtClient.Service.Helpers;

public class RateLimitCoordinator : IRateLimitCoordinator
{
    private readonly ConcurrentDictionary<String, DateTimeOffset> _cooldowns = new();

    public TimeSpan GetRemainingCooldown(String key)
    {
        if (_cooldowns.TryGetValue(key, out var nextAllowedAt))
        {
            var remaining = nextAllowedAt - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                return remaining;
            }
            _cooldowns.TryRemove(key, out _);
        }
        return TimeSpan.Zero;
    }

    public TimeSpan GetMaxRemainingCooldown()
    {
        var now = DateTimeOffset.UtcNow;
        var max = TimeSpan.Zero;
        foreach (var (key, nextAllowedAt) in _cooldowns)
        {
            var remaining = nextAllowedAt - now;
            if (remaining > TimeSpan.Zero)
            {
                if (remaining > max)
                {
                    max = remaining;
                }
            }
            else
            {
                _cooldowns.TryRemove(key, out _);
            }
        }
        return max;
    }

    public DateTimeOffset? GetMaxNextAllowedAt()
    {
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? max = null;
        foreach (var (key, nextAllowedAt) in _cooldowns)
        {
            if (nextAllowedAt > now)
            {
                if (max == null || nextAllowedAt > max)
                {
                    max = nextAllowedAt;
                }
            }
            else
            {
                _cooldowns.TryRemove(key, out _);
            }
        }
        return max;
    }

    public void UpdateCooldown(String key, TimeSpan retryAfter)
    {
        var nextAllowedAt = DateTimeOffset.UtcNow.Add(retryAfter);
        _cooldowns.AddOrUpdate(key, nextAllowedAt, (_, existing) => nextAllowedAt > existing ? nextAllowedAt : existing);
    }

    public DateTimeOffset? GetNextAllowedAt(String key)
    {
        if (_cooldowns.TryGetValue(key, out var nextAllowedAt))
        {
            if (nextAllowedAt > DateTimeOffset.UtcNow)
            {
                return nextAllowedAt;
            }
            _cooldowns.TryRemove(key, out _);
        }
        return null;
    }
}
