namespace RdtClient.Service.Helpers;

public interface IRateLimitCoordinator
{
    TimeSpan GetRemainingCooldown(String key);
    TimeSpan GetMaxRemainingCooldown();
    DateTimeOffset? GetMaxNextAllowedAt();
    void UpdateCooldown(String key, TimeSpan retryAfter);
    DateTimeOffset? GetNextAllowedAt(String key);
}
