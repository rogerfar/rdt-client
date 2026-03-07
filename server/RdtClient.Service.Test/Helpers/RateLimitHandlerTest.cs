using System.Net;
using Moq;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class RateLimitHandlerTest
{
    private readonly Mock<IRateLimitCoordinator> _coordinatorMock = new();

    [Fact]
    public async Task SendAsync_ThrowsRateLimitException_On429WithRetryAfter()
    {
        // Arrange
        var handler = new RateLimitHandler(_coordinatorMock.Object)
        {
            InnerHandler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, 3600)
        };

        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetAsync("http://example.com"));
        Assert.Equal(TimeSpan.FromSeconds(3600), ex.RetryAfter);
        Assert.Contains("rate limit exceeded", ex.Message);
        
        _coordinatorMock.Verify(m => m.UpdateCooldown("example.com", TimeSpan.FromSeconds(3600)), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ThrowsRateLimitException_On429WithoutRetryAfter()
    {
        // Arrange
        var handler = new RateLimitHandler(_coordinatorMock.Object)
        {
            InnerHandler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, null)
        };

        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetAsync("http://example.com"));
        Assert.Equal(TimeSpan.FromMinutes(2), ex.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_ThrowsRateLimitException_WhenCooldownActive()
    {
        // Arrange
        _coordinatorMock.Setup(m => m.GetRemainingCooldown("example.com")).Returns(TimeSpan.FromMinutes(5));
        var handler = new RateLimitHandler(_coordinatorMock.Object)
        {
            InnerHandler = new MockHttpMessageHandler(HttpStatusCode.OK, null)
        };
        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetAsync("http://example.com"));
        Assert.Equal(TimeSpan.FromMinutes(5), ex.RetryAfter);
        Assert.Contains("cooldown active", ex.Message);
    }

    [Fact]
    public async Task SendAsync_DoesNotCatchTimeoutException()
    {
        // Arrange
        var handler = new RateLimitHandler(_coordinatorMock.Object)
        {
            InnerHandler = new MockExceptionHandler(new TimeoutException())
        };
        var client = new HttpClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => client.GetAsync("http://example.com"));
    }

    private class MockHttpMessageHandler(HttpStatusCode statusCode, Int32? retryAfterSeconds) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);

            if (retryAfterSeconds.HasValue)
            {
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(retryAfterSeconds.Value));
            }

            return Task.FromResult(response);
        }
    }

    private class MockExceptionHandler(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw ex;
        }
    }
}
