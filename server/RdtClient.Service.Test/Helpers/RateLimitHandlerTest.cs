using System.Net;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class RateLimitHandlerTest
{
    [Fact]
    public async Task SendAsync_ThrowsRateLimitException_On429WithRetryAfter()
    {
        // Arrange
        var handler = new RateLimitHandler
        {
            InnerHandler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, 3600)
        };
        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetAsync("http://example.com"));
        Assert.Equal(TimeSpan.FromSeconds(3600), ex.RetryAfter);
        Assert.Equal("TorBox rate limit exceeded", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsRateLimitException_On429WithoutRetryAfter()
    {
        // Arrange
        var handler = new RateLimitHandler
        {
            InnerHandler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, null)
        };
        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetAsync("http://example.com"));
        Assert.Equal(TimeSpan.FromMinutes(2), ex.RetryAfter);
    }

    private class MockHttpMessageHandler(HttpStatusCode statusCode, Int32? retryAfterSeconds) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (retryAfterSeconds.HasValue)
            {
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));
            }
            return Task.FromResult(response);
        }
    }
}
