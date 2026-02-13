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
        _loggerMock = new Mock<ILogger<QBittorrent>>();
        _torrentsMock = new Mock<Torrents>(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        _authenticationMock = new Mock<Authentication>(null!, null!, null!);
        
        _qBittorrent = new QBittorrent(_loggerMock.Object, null!, _authenticationMock.Object, _torrentsMock.Object, null!);
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
}
