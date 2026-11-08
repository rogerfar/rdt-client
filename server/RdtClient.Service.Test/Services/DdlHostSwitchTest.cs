using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class DdlHostSwitchTest
{
    private readonly Mock<ITorrentData> _torrentData = new();
    private readonly Mock<IDownloads> _downloads = new();

    private Torrents CreateTorrents()
    {
        return new(new Mock<ILogger<Torrents>>().Object,
                   _torrentData.Object,
                   _downloads.Object,
                   null!,
                   null!,
                   null!,
                   null!,
                   null!,
                   null!,
                   null!,
                   null!,
                   null!,
                   new TestSettings(),
                   new TorrentRunnerState());
    }

    private Torrent DdlTorrent(DateTimeOffset? downloadStarted)
    {
        var torrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Type = DownloadType.Ddl,
            DdlFallbackLinks = "https://nitroflare.com/view/A/x.part1.rar\nhttps://nitroflare.com/view/B/x.part2.rar\nhttps://ddownload.com/c/x.mkv",
            Downloads =
            [
                new()
                {
                    DownloadId = Guid.NewGuid(),
                    Path = "https://rapidgator.net/file/r/x.mkv.html",
                    DownloadStarted = downloadStarted
                }
            ]
        };

        _torrentData.Setup(m => m.GetById(torrent.TorrentId)).ReturnsAsync(torrent);
        _downloads.Setup(m => m.TryAddForTorrent(torrent.TorrentId, It.IsAny<DownloadInfo>())).ReturnsAsync(DownloadAddResult.Added);

        return torrent;
    }

    [Fact]
    public async Task SwitchDdlHost_ReplacesTheDownloadsWithTheNextHostAndKeepsTheRestAsFallback()
    {
        var torrent = DdlTorrent(null);

        var host = await CreateTorrents().SwitchDdlHost(torrent.TorrentId);

        Assert.Equal("nitroflare", host);
        _downloads.Verify(m => m.DeleteForTorrent(torrent.TorrentId), Times.Once);
        _downloads.Verify(m => m.TryAddForTorrent(torrent.TorrentId, It.Is<DownloadInfo>(d => d.RestrictedLink == "https://nitroflare.com/view/A/x.part1.rar")), Times.Once);
        _downloads.Verify(m => m.TryAddForTorrent(torrent.TorrentId, It.Is<DownloadInfo>(d => d.RestrictedLink == "https://nitroflare.com/view/B/x.part2.rar")), Times.Once);
        _torrentData.Verify(m => m.UpdateDdlFallbackLinks(torrent.TorrentId, "https://ddownload.com/c/x.mkv"), Times.Once);
    }

    [Fact]
    public async Task SwitchDdlHost_DoesNothingOnceADownloadHasStarted()
    {
        var torrent = DdlTorrent(DateTimeOffset.UtcNow);

        Assert.Null(await CreateTorrents().SwitchDdlHost(torrent.TorrentId));
        _downloads.Verify(m => m.DeleteForTorrent(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SwitchDdlHost_DoesNothingWithoutFallbackLinks()
    {
        var torrent = DdlTorrent(null);
        torrent.DdlFallbackLinks = null;

        Assert.Null(await CreateTorrents().SwitchDdlHost(torrent.TorrentId));
        _downloads.Verify(m => m.DeleteForTorrent(It.IsAny<Guid>()), Times.Never);
    }
}
