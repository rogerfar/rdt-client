using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class TorrentRunnerTest
{
    [Fact]
    public async Task Tick_ShouldNotRequeueCompletedErrorTorrent()
    {
        var testSettings = new TestSettings();
        var runnerState = new TorrentRunnerState();

        testSettings.Current.Provider.ApiKey = "test-api-key";
        testSettings.Current.Provider.Provider = Provider.RealDebrid;
        testSettings.Current.Provider.MaxParallelDownloads = 1;
        testSettings.Current.DownloadClient.DownloadPath = "/downloads";

        var erroredTorrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Hash = "hash-1",
            RdName = "Torrent 1",
            Payload = new()
            {
                Content = "magnet:?xt=urn:btih:hash-1"
            },
            Type = DownloadType.Torrent,
            RdStatus = TorrentStatus.Queued,
            DeleteOnError = 10,
            Error = "Could not add to provider: Infringing file",
            Completed = DateTimeOffset.UtcNow.AddMinutes(-5),
            Downloads = new List<Download>()
        };

        var torrentDataMock = new Mock<ITorrentData>(MockBehavior.Strict);

        torrentDataMock.Setup(m => m.Get())
                       .ReturnsAsync(new List<Torrent>
                       {
                           erroredTorrent
                       });

        var torrents = new Torrents(Mock.Of<ILogger<Torrents>>(),
                                    torrentDataMock.Object,
                                    Mock.Of<IDownloads>(),
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    testSettings,
                                    runnerState);

        var torrentRunner = new TorrentRunner(Mock.Of<ILogger<TorrentRunner>>(),
                                              torrents,
                                              new(null!),
                                              new(null!, torrents),
                                              Mock.Of<IHttpClientFactory>(),
                                              new RateLimitCoordinator(),
                                              testSettings,
                                              runnerState);

        await torrentRunner.Tick();

        torrentDataMock.Verify(m => m.UpdateComplete(It.IsAny<Guid>(),
                                                     It.IsAny<String?>(),
                                                     It.IsAny<DateTimeOffset?>(),
                                                     It.IsAny<Boolean>()),
                               Times.Never);
    }

    [Fact]
    public void GetLifetimeStart_NonTorBox_IsAdded()
    {
        var added = DateTimeOffset.UtcNow.AddHours(-5);
        var torrent = new Torrent { Added = added, RdAdded = added.AddHours(2), RdStatusRaw = "queued", Type = DownloadType.Torrent };

        Assert.Equal(added, TorrentRunner.GetLifetimeStart(torrent, Provider.RealDebrid));
    }

    [Fact]
    public void GetLifetimeStart_TorBoxQueued_HasNotStarted()
    {
        var torrent = new Torrent { Added = DateTimeOffset.UtcNow.AddHours(-5), RdAdded = DateTimeOffset.UtcNow.AddHours(-5), RdStatusRaw = "queued", Type = DownloadType.Torrent };

        Assert.Null(TorrentRunner.GetLifetimeStart(torrent, Provider.TorBox));
    }

    [Fact]
    public void GetLifetimeStart_TorBoxStartedAfterQueueing_IsProviderCreated()
    {
        var added = DateTimeOffset.UtcNow.AddHours(-5);
        var started = DateTimeOffset.UtcNow.AddMinutes(-10);
        var torrent = new Torrent { Added = added, RdAdded = started, RdStatusRaw = "downloading", Type = DownloadType.Torrent };

        Assert.Equal(started, TorrentRunner.GetLifetimeStart(torrent, Provider.TorBox));
    }

    [Fact]
    public void GetLifetimeStart_TorBoxProviderCreatedBeforeAdded_IsAdded()
    {
        var added = DateTimeOffset.UtcNow.AddHours(-1);
        var torrent = new Torrent { Added = added, RdAdded = added.AddDays(-3), RdStatusRaw = "downloading", Type = DownloadType.Torrent };

        Assert.Equal(added, TorrentRunner.GetLifetimeStart(torrent, Provider.TorBox));
    }

    [Fact]
    public void GetLifetimeStart_TorBoxUsenet_IsAdded()
    {
        var added = DateTimeOffset.UtcNow.AddHours(-5);
        var torrent = new Torrent { Added = added, RdAdded = added.AddHours(2), RdStatusRaw = "queued", Type = DownloadType.Nzb };

        Assert.Equal(added, TorrentRunner.GetLifetimeStart(torrent, Provider.TorBox));
    }
}
