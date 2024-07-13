using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RdtClient.Service.Handlers;

public class RateLimitingHandler : DelegatingHandler
{
    private readonly ConcurrentQueue<DateTimeOffset> _callQueue = new();
    private readonly TimeSpan _requestWindow;
    private readonly ILogger<RateLimitingHandler> _logger;
    private readonly Int32 _requestsAllowedPerWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requestsAllowedPerWindow">The number of requests allowed per window.</param>
    /// <param name="requestWindow">The time to delay when the maximum number of requests has been reached (The window).</param>
    public RateLimitingHandler(ILogger<RateLimitingHandler> logger, Int32 requestsAllowedPerWindow, TimeSpan requestWindow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestsAllowedPerWindow);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(requestWindow, TimeSpan.Zero);
        _logger = logger;
        _requestsAllowedPerWindow = requestsAllowedPerWindow;
        _requestWindow = requestWindow;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        _callQueue.Enqueue(now);
        while (_callQueue.TryPeek(out var oldCall) && now - oldCall > _requestWindow)
        {
            _logger.LogDebug("Removing oldest request from queue");
            _callQueue.TryDequeue(out _);
        }
        if (_callQueue.Count > _requestsAllowedPerWindow)
        {
            _logger.LogDebug("Delaying request");
            var earliestAllowedRequestTime = now.Add(-_requestWindow);
            var delayDuration = _callQueue.ElementAtOrDefault(_requestsAllowedPerWindow - 1) -
                                earliestAllowedRequestTime;
            if (delayDuration > TimeSpan.Zero)
            {
                _logger.LogDebug("Delaying request for {DelayDuration}", delayDuration);
                await Task.Delay(delayDuration, cancellationToken);
            }
        }
        _logger.LogDebug("Sending request");
        return await base.SendAsync(request, cancellationToken);
    }
}