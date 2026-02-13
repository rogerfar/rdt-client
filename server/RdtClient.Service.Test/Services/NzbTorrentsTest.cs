using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using TorrentsService = RdtClient.Service.Services.Torrents;

namespace RdtClient.Service.Test.Services;

public class NzbTorrentsTest
{
    private readonly Mocks _mocks;
    private readonly MockFileSystem _fileSystem;
    private readonly TorrentsService _torrents;

    public NzbTorrentsTest()
    {
        _mocks = new Mocks();
        _fileSystem = new MockFileSystem();
        _torrents = new TorrentsService(
            _mocks.TorrentsLoggerMock.Object,
            _mocks.TorrentDataMock.Object,
            _mocks.DownloadsMock.Object,
            _mocks.ProcessFactoryMock.Object,
            _fileSystem,
            _mocks.EnricherMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!
        );
    }

    [Fact]
    public async Task AddNzbLinkToDebridQueue_ValidLink_AddsToQueue()
    {
        // Arrange
        var nzbLink = "http://example.com/test.nzb";
        var torrent = new Torrent
        {
            DownloadClient = DownloadClient.Bezzad
        };

        _mocks.TorrentDataMock.Setup(t => t.GetByHash(It.IsAny<String>())).ReturnsAsync((Torrent)null!);
        _mocks.TorrentDataMock.Setup(t => t.Add(
            null,
            It.IsAny<String>(),
            nzbLink,
            false,
            DownloadType.Nzb,
            torrent.DownloadClient,
            It.IsAny<Torrent>()
        )).ReturnsAsync(new Torrent { Hash = "mockHash", RdName = "test.nzb" });

        // Act
        var result = await _torrents.AddNzbLinkToDebridQueue(nzbLink, torrent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.nzb", torrent.RdName);
        Assert.Equal(TorrentStatus.Queued, torrent.RdStatus);
        _mocks.TorrentDataMock.Verify(t => t.Add(
            null,
            It.IsAny<String>(),
            nzbLink,
            false,
            DownloadType.Nzb,
            torrent.DownloadClient,
            torrent
        ), Times.Once);
    }

    [Fact]
    public async Task AddNzbLinkToDebridQueue_InvalidLink_ThrowsException()
    {
        // Arrange
        var nzbLink = "invalid-link";
        var torrent = new Torrent();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _torrents.AddNzbLinkToDebridQueue(nzbLink, torrent));
    }

    [Fact]
    public async Task AddNzbFileToDebridQueue_ValidFile_AddsToQueue()
    {
        // Arrange
        var nzbContent = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<nzb xmlns=\"http://www.newzbin.com/DTD/2003/nzb\">\r\n <head>\r\n  <meta type=\"title\">Test NZB Title</meta>\r\n </head>\r\n</nzb>";
        var bytes = Encoding.UTF8.GetBytes(nzbContent);
        var torrent = new Torrent
        {
            DownloadClient = DownloadClient.Bezzad
        };

        _mocks.TorrentDataMock.Setup(t => t.GetByHash(It.IsAny<String>())).ReturnsAsync((Torrent)null!);
        _mocks.TorrentDataMock.Setup(t => t.Add(
            null,
            It.IsAny<String>(),
            It.IsAny<String>(),
            true,
            DownloadType.Nzb,
            torrent.DownloadClient,
            It.IsAny<Torrent>()
        )).ReturnsAsync(new Torrent { Hash = "mockHash", RdName = "Test NZB Title" });

        // Act
        var result = await _torrents.AddNzbFileToDebridQueue(bytes, "filename.nzb", torrent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test NZB Title", torrent.RdName);
        Assert.Equal(TorrentStatus.Queued, torrent.RdStatus);
        _mocks.TorrentDataMock.Verify(t => t.Add(
            null,
            It.IsAny<String>(),
            Convert.ToBase64String(bytes),
            true,
            DownloadType.Nzb,
            torrent.DownloadClient,
            torrent
        ), Times.Once);
    }

    [Fact]
    public async Task AddNzbFileToDebridQueue_InvalidXml_ThrowsException()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("not xml");
        var torrent = new Torrent();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _torrents.AddNzbFileToDebridQueue(bytes, "filename.nzb", torrent));
    }
}
