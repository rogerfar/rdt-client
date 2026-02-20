namespace RdtClient.Data.Models.Internal;

public class RateLimitStatus
{
    public DateTimeOffset? NextDequeueTime { get; set; }
    public Double SecondsRemaining { get; set; }
}
