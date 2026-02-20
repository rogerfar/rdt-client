using System.Net;
using Polly.Timeout;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Helpers;

public class RateLimitHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter;
                var delay = retryAfter?.Delta ?? (retryAfter?.Date.HasValue == true ? retryAfter.Date.Value - DateTimeOffset.UtcNow : TimeSpan.FromMinutes(2));

                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.FromMinutes(2);
                }

                response.Dispose();
                throw new RateLimitException("TorBox rate limit exceeded", delay);
            }

            return response;
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or TaskCanceledException)
        {
            throw new RateLimitException("Provider rate limit exceeded (timeout)", TimeSpan.FromMinutes(2));
        }
    }
}
