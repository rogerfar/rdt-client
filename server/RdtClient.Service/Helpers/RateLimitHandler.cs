using System.Net;
using Polly;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Helpers;

public class RateLimitHandler(IRateLimitCoordinator coordinator) : DelegatingHandler
{
    public static readonly ResiliencePropertyKey<DateTimeOffset> StartTimeKey = new("StartTime");

    public static TimeSpan GetRetryAfterDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        var delay = retryAfter?.Delta ?? (retryAfter?.Date.HasValue == true ? retryAfter.Date.Value - DateTimeOffset.UtcNow : TimeSpan.FromMinutes(2));

        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.FromMinutes(2);
        }

        return delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host ?? "unknown";

        var cooldown = coordinator.GetRemainingCooldown(host);
        if (cooldown > TimeSpan.Zero)
        {
            throw new RateLimitException($"Rate limit cooldown active for {host}", cooldown);
        }

        var context = request.GetResilienceContext();
        if (context == null)
        {
            context = ResilienceContextPool.Shared.Get(cancellationToken);
            request.SetResilienceContext(context);
        }

        if (!context.Properties.TryGetValue(StartTimeKey, out _))
        {
            context.Properties.Set(StartTimeKey, DateTimeOffset.UtcNow);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var delay = GetRetryAfterDelay(response);
            coordinator.UpdateCooldown(host, delay);
            response.Dispose();
            throw new RateLimitException($"Provider {host} rate limit exceeded", delay);
        }

        return response;
    }
}
