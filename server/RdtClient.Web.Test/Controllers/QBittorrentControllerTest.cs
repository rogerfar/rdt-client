using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Models.QBittorrent;
using RdtClient.Service.Services;
using RdtClient.Web.Controllers;

namespace RdtClient.Web.Test.Controllers;

public class QBittorrentControllerTest
{
    private readonly QBittorrentController _controller;
    private readonly Mock<QBittorrent> _qBittorrentMock;

    public QBittorrentControllerTest()
    {
        _qBittorrentMock = new(new Mock<ILogger<QBittorrent>>().Object, null!, null!, null!, null!);

        _controller = new(
            new Mock<ILogger<QBittorrentController>>().Object,
            _qBittorrentMock.Object,
            new Mock<IHttpClientFactory>().Object);

        _controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task TorrentsInfo_FilterAll_DoesNotFilterOutResults()
    {
        // Arrange
        _qBittorrentMock.Setup(q => q.TorrentInfo()).ReturnsAsync(new List<TorrentInfo>
        {
            new()
            {
                Hash = "hash1",
                State = "pausedUP",
                Progress = 1f
            }
        });

        // Act
        var result = await _controller.TorrentsInfo(new()
        {
            Filter = "all",
            Hashes = "hash1"
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IList<TorrentInfo>>(okResult.Value);
        Assert.Single(payload);
        Assert.Equal("hash1", payload[0].Hash);
    }

    [Fact]
    public async Task TorrentsInfo_FilterCompleted_MatchesPausedUploadTorrents()
    {
        // Arrange
        _qBittorrentMock.Setup(q => q.TorrentInfo()).ReturnsAsync(new List<TorrentInfo>
        {
            new()
            {
                Hash = "hash1",
                State = "pausedUP",
                Progress = 1f
            },
            new()
            {
                Hash = "hash2",
                State = "downloading",
                Progress = 0.4f
            }
        });

        // Act
        var result = await _controller.TorrentsInfo(new()
        {
            Filter = "completed"
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IList<TorrentInfo>>(okResult.Value);
        Assert.Single(payload);
        Assert.Equal("hash1", payload[0].Hash);
    }
}