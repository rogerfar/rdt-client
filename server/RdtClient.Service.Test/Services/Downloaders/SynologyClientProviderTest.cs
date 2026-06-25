using Moq;
using RdtClient.Service.Services.Downloaders;
using Synology.Api.Client;

namespace RdtClient.Service.Test.Services.Downloaders;

public class SynologyClientProviderTest
{
    public SynologyClientProviderTest()
    {
        // Static cache is shared across the process; start every test from a clean slate.
        SynologyClientProvider.Reset();
    }

    [Fact]
    public async Task GetClientAsync_ConcurrentCalls_AuthenticatesOnceAndSharesOneClient()
    {
        var logins = 0;
        var shared = new Mock<ISynologyClient>().Object;

        SynologyClientProvider.ClientFactory = (_, _, _) =>
        {
            Interlocked.Increment(ref logins);

            return Task.FromResult(shared);
        };

        var clients = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass")));

        Assert.Equal(1, logins);
        Assert.All(clients, c => Assert.Same(shared, c));

        SynologyClientProvider.Reset();
    }

    [Fact]
    public async Task GetClientAsync_SameCredentials_ReusesClient_DifferentCredentials_ReAuthenticates()
    {
        var logins = 0;

        SynologyClientProvider.ClientFactory = (_, _, _) =>
        {
            Interlocked.Increment(ref logins);

            return Task.FromResult(new Mock<ISynologyClient>().Object);
        };

        await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");
        await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");
        await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "changed");

        Assert.Equal(2, logins);

        SynologyClientProvider.Reset();
    }

    [Fact]
    public async Task InvalidateAsync_ThenGet_AuthenticatesAgainAndLogsOutTheStaleSession()
    {
        var logins = 0;
        var firstMock = new Mock<ISynologyClient>();
        firstMock.Setup(c => c.LogoutAsync()).Returns(Task.CompletedTask);

        var clients = new Queue<ISynologyClient>([firstMock.Object, new Mock<ISynologyClient>().Object]);

        SynologyClientProvider.ClientFactory = (_, _, _) =>
        {
            Interlocked.Increment(ref logins);

            return Task.FromResult(clients.Dequeue());
        };

        var first = await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");
        await SynologyClientProvider.InvalidateAsync(first);
        var second = await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");

        Assert.Equal(2, logins);
        Assert.NotSame(first, second);
        firstMock.Verify(c => c.LogoutAsync(), Times.Once);

        SynologyClientProvider.Reset();
    }

    [Fact]
    public async Task InvalidateAsync_WithAlreadyReplacedClient_DoesNotDropTheCurrentSession()
    {
        var logins = 0;

        SynologyClientProvider.ClientFactory = (_, _, _) =>
        {
            Interlocked.Increment(ref logins);

            return Task.FromResult(new Mock<ISynologyClient>().Object);
        };

        var current = await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");

        // A late failure referencing a session that was already rotated out must not invalidate the live one.
        await SynologyClientProvider.InvalidateAsync(new Mock<ISynologyClient>().Object);

        var afterStaleInvalidate = await SynologyClientProvider.GetClientAsync("http://nas:5000", "user", "pass");

        Assert.Same(current, afterStaleInvalidate);
        Assert.Equal(1, logins);

        SynologyClientProvider.Reset();
    }
}
