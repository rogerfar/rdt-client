using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class SabnzbdTest
{
    private readonly Mock<ILogger<Sabnzbd>> _loggerMock = new();
    private readonly Mock<Torrents> _torrentsMock;
    private readonly AppSettings _appSettings = new() { Port = 6500 };

    public SabnzbdTest()
    {
        _torrentsMock = new Mock<Torrents>(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(new List<Torrent>());
    }

    [Fact]
    public async Task GetQueue_ShouldReturnCorrectStructure()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "Name 1",
                RdProgress = 50,
                Type = DownloadType.Nzb,
                Downloads = new List<Download>
                {
                    new()
                        { BytesTotal = 1000, BytesDone = 500 }
                }
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);
        _torrentsMock.Setup(t => t.GetDownloadStats(It.IsAny<Guid>())).Returns((0, 1000, 500));

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetQueue();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Slots);
        Assert.Equal("hash1", result.Slots[0].NzoId);
        Assert.Equal("Name 1", result.Slots[0].Filename);
        // (50% debrid + 50% download) / 2 = 50%
        Assert.Equal("50", result.Slots[0].Percentage);
    }

    [Fact]
    public async Task GetQueue_ShouldCalculatePercentageCorrectly_WhenDifferentProgress()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "Name 1",
                RdProgress = 100,
                Type = DownloadType.Nzb,
                Downloads = new List<Download>
                {
                    new()
                        { BytesTotal = 1000, BytesDone = 0 }
                }
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetQueue();

        // Assert
        // (100% debrid + 0% download) / 2 = 50%
        Assert.Equal("50", result.Slots[0].Percentage);
    }

    [Fact]
    public async Task GetQueue_ShouldCalculateTimeLeftCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var added = now.AddMinutes(-10);
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "Name 1",
                Added = added,
                RdProgress = 100, // 100% debrid
                Type = DownloadType.Nzb,
                Downloads = new List<Download>
                {
                    new()
                    {
                        BytesTotal = 1000,
                        BytesDone = 0 // 0% download
                    }
                }
            }
        };
        // Overall progress = (1.0 + 0.0) / 2 = 0.5
        // Elapsed = 10 minutes
        // Total estimated = 10 / 0.5 = 20 minutes
        // Time left = 20 - 10 = 10 minutes

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetQueue();

        // Assert
        // Allow for some small variation in time calculation during test execution
        var timeLeftParts = result.Slots[0].TimeLeft.Split(':');
        var hours = Int32.Parse(timeLeftParts[0]);
        var minutes = Int32.Parse(timeLeftParts[1]);
        
        Assert.Equal(0, hours);
        Assert.InRange(minutes, 9, 11);
    }

    [Fact]
    public async Task GetQueue_ShouldCalculateTimeLeftCorrectly_WithRetry()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var added = now.AddMinutes(-20);
        var retry = now.AddMinutes(-10);
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "Name 1",
                Added = added,
                Retry = retry,
                RdProgress = 100, // 100% debrid
                Type = DownloadType.Nzb,
                Downloads = new List<Download>
                {
                    new()
                    {
                        BytesTotal = 1000,
                        BytesDone = 0 // 0% download
                    }
                }
            }
        };
        // Later of Added and Retry is Retry (-10 mins)
        // Overall progress = (1.0 + 0.0) / 2 = 0.5
        // Elapsed = 10 minutes
        // Total estimated = 10 / 0.5 = 20 minutes
        // Time left = 20 - 10 = 10 minutes

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetQueue();

        // Assert
        var timeLeftParts = result.Slots[0].TimeLeft.Split(':');
        var hours = Int32.Parse(timeLeftParts[0]);
        var minutes = Int32.Parse(timeLeftParts[1]);
        
        Assert.Equal(0, hours);
        Assert.InRange(minutes, 9, 11);
    }

    [Fact]
    public async Task GetQueue_ShouldOnlyReturnNzbs()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "NZB 1",
                Type = DownloadType.Nzb,
                Downloads = new List<Download>()
            },
            new()
            {
                Hash = "hash2",
                RdName = "Torrent 1",
                Type = DownloadType.Torrent,
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetQueue();

        // Assert
        Assert.Single(result.Slots);
        Assert.Equal("hash1", result.Slots[0].NzoId);
    }

    [Fact]
    public async Task GetHistory_ShouldOnlyReturnNzbs()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "NZB 1",
                Type = DownloadType.Nzb,
                Completed = DateTimeOffset.UtcNow,
                Downloads = new List<Download>()
            },
            new()
            {
                Hash = "hash2",
                RdName = "Torrent 1",
                Type = DownloadType.Torrent,
                Completed = DateTimeOffset.UtcNow,
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetHistory();

        // Assert
        Assert.Single(result.Slots);
        Assert.Equal("hash1", result.Slots[0].NzoId);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnFullPath()
    {
        // Arrange
        var savePath = @"C:\Downloads";
        Data.Data.SettingData.Get.DownloadClient.MappedPath = savePath;

        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "NZB 1",
                Category = "radarr",
                Type = DownloadType.Nzb,
                Completed = DateTimeOffset.UtcNow,
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetHistory();

        // Assert
        Assert.Single(result.Slots);
        var expectedPath = Path.Combine(savePath, "radarr", "NZB 1");
        Assert.Equal(expectedPath, result.Slots[0].Path);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnFullPath_NoCategory()
    {
        // Arrange
        var savePath = @"C:\Downloads";
        Data.Data.SettingData.Get.DownloadClient.MappedPath = savePath;

        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "NZB 1",
                Category = null,
                Type = DownloadType.Nzb,
                Completed = DateTimeOffset.UtcNow,
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetHistory();

        // Assert
        Assert.Single(result.Slots);
        var expectedPath = Path.Combine(savePath, "NZB 1");
        Assert.Equal(expectedPath, result.Slots[0].Path);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnFailedStatus_WhenTorrentHasError()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                RdName = "NZB 1",
                Type = DownloadType.Nzb,
                Completed = DateTimeOffset.UtcNow,
                Error = "Some error occurred",
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = await sabnzbd.GetHistory();

        // Assert
        Assert.Single(result.Slots);
        Assert.Equal("Failed", result.Slots[0].Status);
    }

    [Fact]
    public void GetConfig_ShouldReturnCorrectConfig()
    {
        // Arrange
        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = sabnzbd.GetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Misc);
        Assert.Equal("6500", result.Misc.Port);
        Assert.NotEmpty(result.Categories);
        Assert.Contains(result.Categories, c => c.Name == "*");
    }

    [Fact]
    public void GetCategories_ShouldOnlyReturnSettingsCategories()
    {
        // Arrange
        var torrentList = new List<Torrent>
        {
            new()
            {
                Hash = "hash1",
                Category = "Movie",
                Type = DownloadType.Nzb,
                Downloads = new List<Download>()
            }
        };

        _torrentsMock.Setup(t => t.Get()).ReturnsAsync(torrentList);

        Data.Data.SettingData.Get.General.Categories = "TV, Music, *";

        var sabnzbd = new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings);

        // Act
        var result = sabnzbd.GetCategories();

        // Assert
        Assert.Equal("*", result[0]);
        Assert.Contains("TV", result);
        Assert.Contains("Music", result);
        Assert.DoesNotContain("Movie", result);
        Assert.Equal(3, result.Count);
    }
}
