using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Test.Services;

/// <summary>
///     Composed test of the OnlyDownloadCached gate: a real Torrents service wired to a
///     real PremiumizeDebridClient over a scripted HTTP handler - only the mocks strictly
///     needed to reach DequeueFromDebridQueue. Proves the gate's observable behavior
///     (rejected-no-transfer vs added), not just its parts.
/// </summary>
public class TorrentsCacheGateTest
{
    private const String CachedJson = """{"status":"success","response":[true],"transcoded":[true],"filename":["Some Release"],"filesize":["1992052865"]}""";
    private const String UncachedJson = """{"status":"success","response":[false],"transcoded":[null],"filename":[null],"filesize":[0]}""";

    private sealed class ScriptedHandler(String cacheCheckJson) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            var uri = request.RequestUri!.ToString();

            var json = uri.Contains("cache/check") ? cacheCheckJson
                : uri.Contains("transfer/create") ? """{"status":"success","id":"transfer-1","name":"test"}"""
                : """{"status":"success","transfers":[]}""";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private static (Torrents Torrents, Mock<ITorrentData> TorrentData, ScriptedHandler Handler) Create(String cacheCheckJson)
    {
        var handler = new ScriptedHandler(cacheCheckJson);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(m => m.CreateClient(It.IsAny<String>())).Returns(() => new HttpClient(handler));

        var settings = new TestSettings();
        settings.Current.Provider.Provider = Provider.Premiumize;
        settings.Current.Provider.ApiKey = "test-api-key";
        settings.Current.Provider.OnlyDownloadCached = true;

        var premiumizeClient = new PremiumizeDebridClient(new Mock<ILogger<PremiumizeDebridClient>>().Object,
                                                          httpClientFactory.Object,
                                                          new Mock<IDownloadableFileFilter>().Object,
                                                          settings);

        var torrentData = new Mock<ITorrentData>();

        var torrents = new Torrents(new Mock<ILogger<Torrents>>().Object,
                                    torrentData.Object,
                                    new Mock<IDownloads>().Object,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    premiumizeClient,
                                    null!,
                                    null!,
                                    null!,
                                    settings,
                                    new Mock<ITorrentRunnerState>().Object);

        return (torrents, torrentData, handler);
    }

    private static Torrent NewTorrent(String hash)
    {
        return new()
        {
            TorrentId = Guid.NewGuid(),
            Hash = hash,
            Type = DownloadType.Torrent,
            IsFile = false
        };
    }

    [Fact]
    public async Task Dequeue_UncachedTorrent_IsRejectedWithoutCreatingATransfer()
    {
        // Arrange
        var (torrents, torrentData, handler) = Create(UncachedJson);
        var torrent = NewTorrent("1111aaaa11111111111111111111111111111111");
        torrentData.Setup(m => m.GetPayloadContent(torrent.TorrentId)).ReturnsAsync($"magnet:?xt=urn:btih:{torrent.Hash}");

        // Act
        await torrents.DequeueFromDebridQueue(torrent);

        // Assert - completed-with-error, never retried, and NO transfer was created
        torrentData.Verify(m => m.UpdateComplete(torrent.TorrentId,
                                                 It.Is<String>(s => s.StartsWith("Rejected")),
                                                 It.IsAny<DateTimeOffset>(),
                                                 false),
                           Times.Once);
        torrentData.Verify(m => m.UpdateRdId(It.IsAny<Torrent>(), It.IsAny<String>()), Times.Never);
        Assert.DoesNotContain(handler.Requests, r => r.ToString().Contains("transfer/create"));
    }

    [Fact]
    public async Task Dequeue_CachedTorrent_IsAddedToTheProvider()
    {
        // Arrange
        var (torrents, torrentData, handler) = Create(CachedJson);
        var torrent = NewTorrent("2222bbbb22222222222222222222222222222222");
        torrentData.Setup(m => m.GetPayloadContent(torrent.TorrentId)).ReturnsAsync($"magnet:?xt=urn:btih:{torrent.Hash}");

        // Act
        await torrents.DequeueFromDebridQueue(torrent);

        // Assert
        torrentData.Verify(m => m.UpdateRdId(torrent, "transfer-1"), Times.Once);
        torrentData.Verify(m => m.UpdateComplete(It.IsAny<Guid>(), It.IsAny<String>(), It.IsAny<DateTimeOffset>(), It.IsAny<Boolean>()), Times.Never);
        Assert.Contains(handler.Requests, r => r.ToString().Contains("transfer/create"));
    }

    [Fact]
    public async Task Dequeue_ReofferedUncachedTorrent_IsAnsweredFromMemoryWithoutASecondCacheCheck()
    {
        // Arrange - two dequeues of the same (unique) hash within the re-check window
        var (torrents, torrentData, handler) = Create(UncachedJson);
        var first = NewTorrent("3333cccc33333333333333333333333333333333");
        var second = NewTorrent(first.Hash!);
        torrentData.Setup(m => m.GetPayloadContent(It.IsAny<Guid>())).ReturnsAsync($"magnet:?xt=urn:btih:{first.Hash}");

        // Act
        await torrents.DequeueFromDebridQueue(first);
        await torrents.DequeueFromDebridQueue(second);

        // Assert - both rejected, but the provider was asked exactly once
        torrentData.Verify(m => m.UpdateComplete(It.IsAny<Guid>(),
                                                 It.Is<String>(s => s.StartsWith("Rejected")),
                                                 It.IsAny<DateTimeOffset>(),
                                                 false),
                           Times.Exactly(2));
        Assert.Single(handler.Requests, r => r.ToString().Contains("cache/check"));
    }
}
