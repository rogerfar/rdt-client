namespace RdtClient.Data.Models.Internal;

public class RateLimitException(String message, TimeSpan retryAfter) : Exception(message)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
