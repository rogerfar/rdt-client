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
        var originalApiKey = Settings.Get.Provider.ApiKey;
        var originalProvider = Settings.Get.Provider.Provider;
        var originalMaxParallelDownloads = Settings.Get.Provider.MaxParallelDownloads;
        var originalDownloadPath = Settings.Get.DownloadClient.DownloadPath;

        TorrentRunner.ActiveDownloadClients.Clear();
        TorrentRunner.ActiveUnpackClients.Clear();

        try
        {
            Settings.Get.Provider.ApiKey = "test-api-key";
            Settings.Get.Provider.Provider = Provider.RealDebrid;
            Settings.Get.Provider.MaxParallelDownloads = 1;
            Settings.Get.DownloadClient.DownloadPath = "/downloads";

            var erroredTorrent = new Torrent
            {
                TorrentId = Guid.NewGuid(),
                Hash = "hash-1",
                RdName = "Torrent 1",
                FileOrMagnet = "magnet:?xt=urn:btih:hash-1",
                Type = DownloadType.Torrent,
                RdStatus = TorrentStatus.Queued,
                DeleteOnError = 10,
                Error = "Could not add to provider: Infringing file",
                Completed = DateTimeOffset.UtcNow.AddMinutes(-5),
                Downloads = new List<Download>()
            };

            var torrentDataMock = new Mock<ITorrentData>(MockBehavior.Strict);
            torrentDataMock.Setup(m => m.Get()).ReturnsAsync(new List<Torrent>
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
                                        null!);

            var torrentRunner = new TorrentRunner(Mock.Of<ILogger<TorrentRunner>>(),
                                                  torrents,
                                                  new(null!),
                                                  new(null!, torrents),
                                                  Mock.Of<IHttpClientFactory>(),
                                                  new RateLimitCoordinator());

            await torrentRunner.Tick();

            torrentDataMock.Verify(m => m.UpdateComplete(It.IsAny<Guid>(),
                                                         It.IsAny<String?>(),
                                                         It.IsAny<DateTimeOffset?>(),
                                                         It.IsAny<Boolean>()),
                                   Times.Never);
        }
        finally
        {
            Settings.Get.Provider.ApiKey = originalApiKey;
            Settings.Get.Provider.Provider = originalProvider;
            Settings.Get.Provider.MaxParallelDownloads = originalMaxParallelDownloads;
            Settings.Get.DownloadClient.DownloadPath = originalDownloadPath;
            TorrentRunner.ActiveDownloadClients.Clear();
            TorrentRunner.ActiveUnpackClients.Clear();
        }
    }
}