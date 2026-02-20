using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;
using TorBoxNET;

namespace RdtClient.Service.Test.Services.TorrentClients;

public class TorBoxDebridClientTest
{
    private readonly Mock<ILogger<TorBoxDebridClient>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IDownloadableFileFilter> _fileFilterMock;

    public TorBoxDebridClientTest()
    {
        _loggerMock = new();
        _httpClientFactoryMock = new();
        _fileFilterMock = new();
        
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<String>())).Returns(httpClient);
        
        Settings.Get.Provider.ApiKey = "test-api-key";
        Settings.Get.Provider.Timeout = 100;
    }

    [Fact]
    public async Task GetDownloads_ReturnsTorrentsAndNzbsWithCorrectType()
    {
        // Arrange
        var torrents = new List<TorrentInfoResult>
        {
            new() { Hash = "hash1", Name = "torrent1", Size = 1000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        var nzbs = new List<UsenetInfoResult>
        {
            new() { Hash = "hash2", Name = "nzb1", Size = 2000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        clientMock.Protected().Setup<Task<IEnumerable<TorrentInfoResult>?>>("GetCurrentTorrents").ReturnsAsync(torrents);
        clientMock.Protected().Setup<Task<IEnumerable<TorrentInfoResult>?>>("GetQueuedTorrents").ReturnsAsync(new List<TorrentInfoResult>());
        clientMock.Protected().Setup<Task<IEnumerable<UsenetInfoResult>?>>("GetCurrentUsenet").ReturnsAsync(nzbs);
        clientMock.Protected().Setup<Task<IEnumerable<UsenetInfoResult>?>>("GetQueuedUsenet").ReturnsAsync(new List<UsenetInfoResult>());

        // Act
        var result = await clientMock.Object.GetDownloads();

        // Assert
        Assert.Equal(2, result.Count);
        
        var torrentResult = result.FirstOrDefault(r => r.Id == "hash1");
        Assert.NotNull(torrentResult);
        Assert.Equal(DownloadType.Torrent, torrentResult.Type);
        
        var nzbResult = result.FirstOrDefault(r => r.Id == "hash2");
        Assert.NotNull(nzbResult);
        Assert.Equal(DownloadType.Nzb, nzbResult.Type);
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsTorrentFiles_WhenTorrentFound()
    {
        // Arrange
        var hash = "test-hash";
        var availability = new Response<List<AvailableTorrent?>>
        {
            Data = new()
            {
                new()
                {
                    Files = new()
                    {
                        new() { Name = "file1.mkv", Size = 100 },
                        new() { Name = "file2.txt", Size = 10 }
                    }
                }
            }
        };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        clientMock.Protected().Setup<Task<Response<List<AvailableTorrent?>>>>("GetTorrentAvailability", hash).ReturnsAsync(availability);

        // Act
        var result = await clientMock.Object.GetAvailableFiles(hash);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("file1.mkv", result[0].Filename);
        Assert.Equal(100, result[0].Filesize);
        Assert.Equal("file2.txt", result[1].Filename);
        Assert.Equal(10, result[1].Filesize);
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsUsenetFiles_WhenTorrentNotFoundButUsenetFound()
    {
        // Arrange
        var hash = "test-hash";
        var torrentAvailability = new Response<List<AvailableTorrent?>> { Data = new() };
        var usenetAvailability = new Response<List<AvailableUsenet?>>
        {
            Data = new()
            {
                new()
                {
                    Files = new()
                    {
                        new() { Name = "file1.nzb", Size = 200 }
                    }
                }
            }
        };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        clientMock.Protected().Setup<Task<Response<List<AvailableTorrent?>>>>("GetTorrentAvailability", hash).ReturnsAsync(torrentAvailability);
        clientMock.Protected().Setup<Task<Response<List<AvailableUsenet?>>>>("GetUsenetAvailability", hash).ReturnsAsync(usenetAvailability);

        // Act
        var result = await clientMock.Object.GetAvailableFiles(hash);

        // Assert
        Assert.Single(result);
        Assert.Equal("file1.nzb", result[0].Filename);
        Assert.Equal(200, result[0].Filesize);
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsEmptyList_WhenNeitherFound()
    {
        // Arrange
        var hash = "test-hash";
        var torrentAvailability = new Response<List<AvailableTorrent?>> { Data = new() };
        var usenetAvailability = new Response<List<AvailableUsenet?>> { Data = new() };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        clientMock.Protected().Setup<Task<Response<List<AvailableTorrent?>>>>("GetTorrentAvailability", hash).ReturnsAsync(torrentAvailability);
        clientMock.Protected().Setup<Task<Response<List<AvailableUsenet?>>>>("GetUsenetAvailability", hash).ReturnsAsync(usenetAvailability);

        // Act
        var result = await clientMock.Object.GetAvailableFiles(hash);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Delete_CallsTorrentsControl_WhenTypeIsTorrent()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "torrent-id",
            Type = DownloadType.Torrent
        };

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);

        // Act
        await clientMock.Object.Delete(torrent);

        // Assert
        torrentsApiMock.Verify(m => m.ControlAsync("torrent-id", "delete", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsUsenetControl_WhenTypeIsNzb()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "nzb-id",
            Type = DownloadType.Nzb
        };

        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);

        // Act
        await clientMock.Object.Delete(torrent);

        // Assert
        usenetApiMock.Verify(m => m.ControlAsync("nzb-id", "delete", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unrestrict_CallsTorrentsRequestDownload_WhenTypeIsTorrent()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "torrent-id",
            Type = DownloadType.Torrent
        };
        var link = "https://torbox.app/d/123/456";

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        torrentsApiMock.Setup(m => m.RequestDownloadAsync(123, 456, false, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Response<String> { Data = "https://unrestricted-link" });

        // Act
        var result = await clientMock.Object.Unrestrict(torrent, link);

        // Assert
        Assert.Equal("https://unrestricted-link", result);
        torrentsApiMock.Verify(m => m.RequestDownloadAsync(123, 456, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unrestrict_CallsUsenetRequestDownload_WhenTypeIsNzb()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "nzb-id",
            Type = DownloadType.Nzb
        };
        var link = "https://torbox.app/d/123/456";

        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.RequestDownloadAsync(123, 456, false, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Response<String> { Data = "https://unrestricted-link-nzb" });

        // Act
        var result = await clientMock.Object.Unrestrict(torrent, link);

        // Assert
        Assert.Equal("https://unrestricted-link-nzb", result);
        usenetApiMock.Verify(m => m.RequestDownloadAsync(123, 456, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddNzbFile_CallsUsenetAddFileAsyncWithName()
    {
        // Arrange
        var bytes = new Byte[] { 1, 2, 3 };
        var name = "test-nzb";
        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.AddFileAsync(bytes, It.IsAny<Int32>(), name, It.IsAny<String?>(), It.IsAny<Boolean>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Response<UsenetAddResult> { Data = new UsenetAddResult { Hash = "new-hash" } });

        // Act
        var result = await clientMock.Object.AddNzbFile(bytes, name);

        // Assert
        Assert.Equal("new-hash", result);
        usenetApiMock.Verify(m => m.AddFileAsync(bytes, It.IsAny<Int32>(), name, It.IsAny<String?>(), It.IsAny<Boolean>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GetDownloadInfos_GeneratesCorrectFakedlLinks_ForIndividualFiles()
    {
        // Arrange
        var files = new List<DebridClientFile>
        {
            new() { Id = 1, Path = "file1.mkv", Bytes = 1000 },
            new() { Id = 2, Path = "file2.mkv", Bytes = 2000 }
        };
        var torrent = new Torrent
        {
            Hash = "test-hash",
            RdFiles = JsonConvert.SerializeObject(files)
        };

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        torrentsApiMock.Setup(m => m.GetHashInfoAsync("test-hash", true, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new TorrentInfoResult { Id = 12345 });

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        Settings.Get.Provider.PreferZippedDownloads = false;

        // Act
        var result = await clientMock.Object.GetDownloadInfos(torrent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("https://torbox.app/fakedl/12345/1", result[0].RestrictedLink);
        Assert.Equal("https://torbox.app/fakedl/12345/2", result[1].RestrictedLink);
    }

    [Fact]
    public async Task GetDownloadInfos_GeneratesCorrectFakedlLink_ForZipDownload()
    {
        // Arrange
        var files = new List<DebridClientFile>
        {
            new() { Id = 1, Path = "file1.mkv", Bytes = 1000 }
        };
        var torrent = new Torrent
        {
            Hash = "test-hash",
            RdName = "TestTorrent",
            RdFiles = JsonConvert.SerializeObject(files),
            DownloadClient = Data.Enums.DownloadClient.Aria2c
        };

        Settings.Get.Provider.PreferZippedDownloads = true;

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        torrentsApiMock.Setup(m => m.GetHashInfoAsync("test-hash", true, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new TorrentInfoResult { Id = 12345 });

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        // Act
        var result = await clientMock.Object.GetDownloadInfos(torrent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("https://torbox.app/fakedl/12345/zip", result[0].RestrictedLink);
        Assert.Equal("TestTorrent.zip", result[0].FileName);
    }

    [Fact]
    public async Task Unrestrict_ParsesFakedlLinksCorrectly_ForIndividualFiles()
    {
        // Arrange
        var torrent = new Torrent
        {
            Type = DownloadType.Torrent
        };
        var link = "https://torbox.app/fakedl/12345/6789";

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        torrentsApiMock.Setup(m => m.RequestDownloadAsync(12345, 6789, false, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Response<String> { Data = "https://real-download-link" });

        // Act
        var result = await clientMock.Object.Unrestrict(torrent, link);

        // Assert
        Assert.Equal("https://real-download-link", result);
        torrentsApiMock.Verify(m => m.RequestDownloadAsync(12345, 6789, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unrestrict_ParsesFakedlLinksCorrectly_ForZipDownload()
    {
        // Arrange
        var torrent = new Torrent
        {
            Type = DownloadType.Torrent
        };
        var link = "https://torbox.app/fakedl/12345/zip";

        var torrentsApiMock = new Mock<ITorrentsApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        torrentsApiMock.Setup(m => m.RequestDownloadAsync(12345, 0, true, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Response<String> { Data = "https://real-zip-download-link" });

        // Act
        var result = await clientMock.Object.Unrestrict(torrent, link);

        // Assert
        Assert.Equal("https://real-zip-download-link", result);
        torrentsApiMock.Verify(m => m.RequestDownloadAsync(12345, 0, true, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GetDownloadInfos_GeneratesCorrectFakedlLinks_ForUsenet()
    {
        // Arrange
        var files = new List<DebridClientFile>
        {
            new() { Id = 1, Path = "file1.nzb", Bytes = 1000 }
        };
        var torrent = new Torrent
        {
            Type = DownloadType.Nzb,
            RdId = "nzb-hash",
            RdFiles = JsonConvert.SerializeObject(files)
        };

        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.GetCurrentAsync(true, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<UsenetInfoResult> { new() { Hash = "nzb-hash", Id = 98765 } });

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        Settings.Get.Provider.PreferZippedDownloads = false;

        // Act
        var result = await clientMock.Object.GetDownloadInfos(torrent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("https://torbox.app/fakedl/98765/1", result[0].RestrictedLink);
    }

    [Fact]
    public async Task Unrestrict_ParsesFakedlLinksCorrectly_ForUsenet()
    {
        // Arrange
        var torrent = new Torrent
        {
            Type = DownloadType.Nzb
        };
        var link = "https://torbox.app/fakedl/98765/4321";

        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.RequestDownloadAsync(98765, 4321, false, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Response<String> { Data = "https://real-usenet-link" });

        // Act
        var result = await clientMock.Object.Unrestrict(torrent, link);

        // Assert
        Assert.Equal("https://real-usenet-link", result);
        usenetApiMock.Verify(m => m.RequestDownloadAsync(98765, 4321, false, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task AddTorrentMagnet_ThrowsRateLimitException_OnActiveLimit()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:test";
        var torrentsApiMock = new Mock<ITorrentsApi>();
        var userApiMock = new Mock<IUserApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        torBoxClientMock.Setup(m => m.User).Returns(userApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        userApiMock.Setup(m => m.GetAsync(true, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Response<User> { Data = new User { Settings = new UserSettings { SeedTorrents = 5 } } });

        torrentsApiMock.Setup(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new TorBoxException("active_limit", "Active limit reached"));
        
        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddTorrentMagnet(magnetLink));
        torrentsApiMock.Verify(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()), Times.Once);
        torrentsApiMock.Verify(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddTorrentMagnet_ThrowsRateLimitException_OnSlowDown()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:test";
        var torrentsApiMock = new Mock<ITorrentsApi>();
        var userApiMock = new Mock<IUserApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        torBoxClientMock.Setup(m => m.User).Returns(userApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        userApiMock.Setup(m => m.GetAsync(true, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Response<User> { Data = new User { Settings = new UserSettings { SeedTorrents = 5 } } });

        torrentsApiMock.Setup(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("slow_down"));

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddTorrentMagnet(magnetLink));
    }

    [Fact]
    public async Task AddTorrentMagnet_ThrowsRateLimitException_OnRateLimitException()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:test";
        var torrentsApiMock = new Mock<ITorrentsApi>();
        var userApiMock = new Mock<IUserApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        torBoxClientMock.Setup(m => m.User).Returns(userApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        userApiMock.Setup(m => m.GetAsync(true, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Response<User> { Data = new User { Settings = new UserSettings { SeedTorrents = 5 } } });

        torrentsApiMock.Setup(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new RateLimitException("TorBox rate limit exceeded", TimeSpan.FromMinutes(60)));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddTorrentMagnet(magnetLink));
        Assert.Equal(TimeSpan.FromMinutes(60), ex.RetryAfter);
    }

    [Fact]
    public async Task AddTorrentMagnet_ThrowsRateLimitException_OnWrappedRateLimitException()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:test";
        var torrentsApiMock = new Mock<ITorrentsApi>();
        var userApiMock = new Mock<IUserApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        torBoxClientMock.Setup(m => m.User).Returns(userApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        userApiMock.Setup(m => m.GetAsync(true, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Response<User> { Data = new User { Settings = new UserSettings { SeedTorrents = 5 } } });

        torrentsApiMock.Setup(m => m.AddMagnetAsync(magnetLink, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("Wrapped", new RateLimitException("TorBox rate limit exceeded", TimeSpan.FromMinutes(60))));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddTorrentMagnet(magnetLink));
        Assert.Equal(TimeSpan.FromMinutes(60), ex.RetryAfter);
    }

    [Fact]
    public async Task AddTorrentFile_ThrowsRateLimitException_OnActiveLimit()
    {
        // Arrange
        var bytes = new Byte[] { 1, 2, 3 };
        var torrentsApiMock = new Mock<ITorrentsApi>();
        var userApiMock = new Mock<IUserApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Torrents).Returns(torrentsApiMock.Object);
        torBoxClientMock.Setup(m => m.User).Returns(userApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        userApiMock.Setup(m => m.GetAsync(true, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Response<User> { Data = new User { Settings = new UserSettings { SeedTorrents = 5 } } });

        torrentsApiMock.Setup(m => m.AddFileAsync(bytes, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new TorBoxException("active_limit", "Active limit reached"));

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddTorrentFile(bytes));
        torrentsApiMock.Verify(m => m.AddFileAsync(bytes, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()), Times.Once);
        torrentsApiMock.Verify(m => m.AddFileAsync(bytes, 5, It.IsAny<Boolean>(), It.IsAny<String?>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddNzbLink_ThrowsRateLimitException_OnActiveLimit()
    {
        // Arrange
        var nzbLink = "https://example.com/test.nzb";
        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.AddLinkAsync(nzbLink, It.IsAny<Int32>(), It.IsAny<String?>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new TorBoxException("active_limit", "Active limit reached"));

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddNzbLink(nzbLink));
        usenetApiMock.Verify(m => m.AddLinkAsync(nzbLink, It.IsAny<Int32>(), It.IsAny<String?>(), It.IsAny<String?>(), false, It.IsAny<CancellationToken>()), Times.Once);
        usenetApiMock.Verify(m => m.AddLinkAsync(nzbLink, It.IsAny<Int32>(), It.IsAny<String?>(), It.IsAny<String?>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddNzbFile_ThrowsRateLimitException_OnActiveLimit()
    {
        // Arrange
        var bytes = new Byte[] { 1, 2, 3 };
        var name = "test.nzb";
        var usenetApiMock = new Mock<IUsenetApi>();
        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object) { CallBase = true };
        var torBoxClientMock = new Mock<ITorBoxNetClient>();
        
        torBoxClientMock.Setup(m => m.Usenet).Returns(usenetApiMock.Object);
        clientMock.Protected().Setup<ITorBoxNetClient>("GetClient").Returns(torBoxClientMock.Object);
        
        usenetApiMock.Setup(m => m.AddFileAsync(bytes, It.IsAny<Int32>(), name, It.IsAny<String?>(), false, It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new TorBoxException("active_limit", "Active limit reached"));

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() => clientMock.Object.AddNzbFile(bytes, name));
        usenetApiMock.Verify(m => m.AddFileAsync(bytes, It.IsAny<Int32>(), name, It.IsAny<String?>(), false, It.IsAny<CancellationToken>()), Times.Once);
        usenetApiMock.Verify(m => m.AddFileAsync(bytes, It.IsAny<Int32>(), name, It.IsAny<String?>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateData_SetsErrorStatus_WhenTorBoxStatusStartsWithFailed()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "test-rd-id",
            RdStatus = TorrentStatus.Downloading
        };
        var torrentClientTorrent = new DebridClientTorrent
        {
            Status = "failed (Aborted, cannot be completed - https://sabnzbd.org/not-complete)",
            Filename = "test-file"
        };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);

        // Act
        var result = await clientMock.Object.UpdateData(torrent, torrentClientTorrent);

        // Assert
        Assert.Equal(TorrentStatus.Error, result.RdStatus);
    }

    [Fact]
    public async Task UpdateData_LogsWarning_WhenTorBoxStatusIsUnmapped()
    {
        // Arrange
        var torrent = new Torrent
        {
            RdId = "test-rd-id",
            RdStatus = TorrentStatus.Downloading,
            RdName = "test-torrent"
        };
        var torrentClientTorrent = new DebridClientTorrent
        {
            Status = "some-unknown-status",
            Filename = "test-torrent"
        };

        var clientMock = new Mock<TorBoxDebridClient>(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object);

        // Act
        await clientMock.Object.UpdateData(torrent, torrentClientTorrent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unmapped status") && v.ToString()!.Contains("some-unknown-status")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, String>>((v, t) => true)),
            Times.Once);
    }
}
