using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class QBittorrentTest
{
    private readonly Mock<ILogger<QBittorrent>> _loggerMock;
    private readonly Mock<Torrents> _torrentsMock;
    private readonly Mock<Authentication> _authenticationMock;
    private readonly QBittorrent _qBittorrent;

    public QBittorrentTest()
    {
        _loggerMock = new();
        _torrentsMock = new(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        _authenticationMock = new(null!, null!, null!);
        
        _qBittorrent = new(_loggerMock.Object, null!, _authenticationMock.Object, _torrentsMock.Object, null!);
    }

    [Fact]
    public async Task TorrentInfo_ShouldOnlyReturnTorrents()
    {
        // Arrange
        var allTorrents = new List<Torrent>
        {
            new()
            {
                TorrentId = Guid.NewGuid(),
                Hash = "hash1",
                RdName = "Torrent 1",
                Type = DownloadType.Torrent
            },
            new()
            {
                TorrentId = Guid.NewGuid(),
                Hash = "hash2",
                RdName = "NZB 1",
                Type = DownloadType.Nzb
            }
        };

        _torrentsMock.Setup(m => m.Get()).ReturnsAsync(allTorrents);

        // Act
        var result = await _qBittorrent.TorrentInfo();

        // Assert
        Assert.Single(result);
        Assert.Equal("hash1", result[0].Hash);
        Assert.Equal("Torrent 1", result[0].Name);
    }
    
    [Fact]
    public async Task TorrentInfo_ShouldReport100Percent_WhenDownloadIsComplete()
    {
        // Arrange
        var downloadId = Guid.NewGuid();
        var torrentId = Guid.NewGuid();
        var allTorrents = new List<Torrent>
        {
            new()
            {
                TorrentId = torrentId,
                Hash = "hash1",
                RdName = "Torrent 1",
                RdProgress = 100, // Real-Debrid is 100%
                Type = DownloadType.Torrent,
                Downloads = new List<Download>
                {
                    new() { DownloadId = downloadId, TorrentId = torrentId }
                }
            }
        };

        _torrentsMock.Setup(m => m.Get()).ReturnsAsync(allTorrents);
        // Local download is also 100%
        _torrentsMock.Setup(m => m.GetDownloadStats(downloadId)).Returns((0, 1000, 1000));

        // Act
        var result = await _qBittorrent.TorrentInfo();

        // Assert
        Assert.Single(result);
        Assert.Equal(1.0f, result[0].Progress);
    }

    [Fact]
    public async Task TorrentInfo_ShouldReport90Percent_WhenRDIs100AndLocalIs80()
    {
        // Arrange
        var downloadId = Guid.NewGuid();
        var torrentId = Guid.NewGuid();
        var allTorrents = new List<Torrent>
        {
            new()
            {
                TorrentId = torrentId,
                Hash = "hash1",
                RdName = "Torrent 1",
                RdProgress = 100, // Real-Debrid is 100%
                Type = DownloadType.Torrent,
                Downloads = new List<Download>
                {
                    new() { DownloadId = downloadId, TorrentId = torrentId }
                }
            }
        };

        _torrentsMock.Setup(m => m.Get()).ReturnsAsync(allTorrents);
        // Local download is 80%
        _torrentsMock.Setup(m => m.GetDownloadStats(downloadId)).Returns((0, 1000, 800));

        // Act
        var result = await _qBittorrent.TorrentInfo();

        // Assert
        Assert.Single(result);
        // Current behavior is (1.0 + 0.8) / 2 = 0.9
        Assert.Equal(0.9f, result[0].Progress);
    }
}
