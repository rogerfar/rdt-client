using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;
using DownloadClient = RdtClient.Data.Enums.DownloadClient;
using Transfer = RdtClient.Service.Services.DebridClients.HighwayDebridClient.HighwayTransfer;
using Stats = RdtClient.Service.Services.DebridClients.HighwayDebridClient.HighwayStats;
using CheckResponse = RdtClient.Service.Services.DebridClients.HighwayDebridClient.HighwayCheckResponse;

namespace RdtClient.Service.Test.Services.TorrentClients;

public class HighwayDebridClientTest
{
    private readonly Mock<IDownloadableFileFilter> _fileFilterMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<HighwayDebridClient>> _loggerMock = new();
    private readonly TestSettings _settings = new();

    public HighwayDebridClientTest()
    {
        _settings.Current.Provider.ApiKey = "3f9a1c2b4d5e6f708192a3b4c5d6e7f8";
        _settings.Current.Provider.Timeout = 100;
    }

    private Mock<HighwayDebridClient> CreateClient()
    {
        return new(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object, _settings);
    }

    [Fact]
    public async Task GetDownloads_MapsServiceToDownloadType()
    {
        var transfers = new List<Transfer>
        {
            new()
            {
                Id = "4213",
                Service = "torrent",
                Name = "Torrent.Release",
                State = "downloading",
                Progress = 62,
                Size = 100
            },
            new()
            {
                Id = "3f9a1c2b4d5e6f708192a3b4c5d6e7f8",
                Service = "usenet",
                Name = "Usenet.Release",
                State = "queued"
            }
        };

        var clientMock = CreateClient();
        clientMock.Protected().Setup<Task<IList<Transfer>>>("ListTransfers").ReturnsAsync(transfers);

        var result = await clientMock.Object.GetDownloads();

        Assert.Equal(2, result.Count);
        Assert.Equal(DownloadType.Torrent, result.Single(r => r.Id == "4213").Type);
        Assert.Equal(DownloadType.Nzb, result.Single(r => r.Id == "3f9a1c2b4d5e6f708192a3b4c5d6e7f8").Type);
    }

    [Theory]
    [InlineData("4213")]
    [InlineData("3f9a1c2b4d5e6f708192a3b4c5d6e7f8")]
    public async Task AddTorrentMagnet_ReturnsTransferId(String id)
    {
        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("PostTransfer", ItExpr.IsAny<HttpContent>())
                  .ReturnsAsync(new Transfer
                  {
                      Id = id,
                      Service = "torrent",
                      State = "queued"
                  });

        var result = await clientMock.Object.AddTorrentMagnet("magnet:?xt=urn:btih:0123");

        Assert.Equal(id, result);
    }

    [Fact]
    public async Task AddTorrentMagnet_Throws_WhenNoIdReturned()
    {
        var clientMock = CreateClient();
        clientMock.Protected().Setup<Task<Transfer?>>("PostTransfer", ItExpr.IsAny<HttpContent>()).ReturnsAsync((Transfer?) null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => clientMock.Object.AddTorrentMagnet("magnet:?xt=urn:btih:0123"));
    }

    [Theory]
    [InlineData("queued", TorrentStatus.Processing)]
    [InlineData("downloading", TorrentStatus.Downloading)]
    [InlineData("completed", TorrentStatus.Finished)]
    [InlineData("error", TorrentStatus.Error)]
    public async Task UpdateData_MapsState(String state, TorrentStatus expected)
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var cached = new DebridClientTorrent
        {
            Id = "4213",
            Filename = "Some.Release",
            Status = state,
            Message = "boom",
            Bytes = 8123,
            Progress = 50
        };

        var result = await CreateClient().Object.UpdateData(torrent, cached);

        Assert.Equal(expected, result.RdStatus);

        if (state == "error")
        {
            Assert.Equal("boom", result.Error);
        }
    }

    [Fact]
    public async Task UpdateData_StoresContentPathInRdHost()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var cached = new DebridClientTorrent
        {
            Id = "4213",
            Filename = "Some.Release",
            Status = "downloading",
            Host = "TORRENT/Some.Release"
        };

        var result = await CreateClient().Object.UpdateData(torrent, cached);

        Assert.Equal("TORRENT/Some.Release", result.RdHost);
        Assert.Equal(Provider.HIGHWAY, result.ClientKind);
    }

    [Fact]
    public async Task UpdateData_MarksDeleted_WhenTransferNotFound()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("GetTransfer", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync((Transfer?) null);

        var result = await clientMock.Object.UpdateData(torrent, null);

        Assert.Equal("deleted", result.RdStatusRaw);
    }

    [Fact]
    public async Task SelectFiles_RetriesSymlink_WhenFilesNotYetIndexed()
    {
        // HIGHWAY can report state:"completed" with files:[] (cloud_files index lag, esp. usenet, spec §8).
        // Symlink mode needs named files to symlink, so it must keep waiting.
        var torrent = new Torrent
        {
            DownloadClient = DownloadClient.Symlink
        };

        var result = await CreateClient().Object.SelectFiles(torrent);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(DownloadClient.Bezzad)]
    [InlineData(DownloadClient.Aria2c)]
    [InlineData(DownloadClient.DownloadStation)]
    public async Task SelectFiles_DoesNotBlockNonSymlinkClients_WhenFilesNotYetIndexed(DownloadClient downloadClient)
    {
        // Non-symlink clients always fetch the whole transfer as one ZIP (GetDownloadInfos), so a missing
        // files[] index must not stall CreateDownloads for them.
        var torrent = new Torrent
        {
            DownloadClient = downloadClient
        };

        var result = await CreateClient().Object.SelectFiles(torrent);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task SelectFiles_ReturnsActualFileCount_WhenFilesIndexed()
    {
        var torrent = new Torrent
        {
            DownloadClient = DownloadClient.Symlink,
            RdFiles = JsonSerializer.Serialize(new List<DebridClientFile>
            {
                new()
                {
                    Id = 0,
                    Path = "a.mkv",
                    Selected = true
                },
                new()
                {
                    Id = 1,
                    Path = "b.nfo",
                    Selected = true
                }
            })
        };

        var result = await CreateClient().Object.SelectFiles(torrent);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetDownloadInfos_Symlink_ReturnsPerFileSentinels_SkippingPartial()
    {
        var files = new List<DebridClientFile>
        {
            new()
            {
                Id = 0,
                Path = "Some.Release.mkv",
                Bytes = 8120,
                Selected = true
            },
            new()
            {
                Id = 1,
                Path = "Some.Release.part",
                Bytes = 10,
                Selected = false
            }
        };

        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Symlink,
            RdFiles = JsonSerializer.Serialize(files)
        };

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("highway://4213/torrent/0", result[0].RestrictedLink);
        Assert.Equal("Some.Release.mkv", result[0].FileName);
    }

    [Fact]
    public async Task GetDownloadInfos_NonSymlink_ReturnsPerFileDownloadInfos_WhenDownloadUrlsPresent()
    {
        var files = new List<DebridClientFile>
        {
            new()
            {
                Id = 0,
                Path = "a.mkv",
                Bytes = 8120,
                Selected = true,
                DownloadLink = "https://nl.dwld.link/dav/TORRENT/Some.Release/a.mkv?sig=1"
            },
            new()
            {
                Id = 1,
                Path = "b.nfo",
                Bytes = 42,
                Selected = true,
                DownloadLink = "https://nl.dwld.link/dav/TORRENT/Some.Release/b.nfo?sig=2"
            }
        };

        var torrent = new Torrent
        {
            RdId = "4213",
            RdName = "Some.Release",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Bezzad,
            RdFiles = JsonSerializer.Serialize(files)
        };

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("https://nl.dwld.link/dav/TORRENT/Some.Release/a.mkv?sig=1", result[0].RestrictedLink);
        Assert.Equal("a.mkv", result[0].FileName);
        Assert.Equal("https://nl.dwld.link/dav/TORRENT/Some.Release/b.nfo?sig=2", result[1].RestrictedLink);
        Assert.Equal("b.nfo", result[1].FileName);
    }

    [Fact]
    public async Task GetDownloadInfos_NonSymlink_FallsBackToZipSentinel_WhenNoDownloadUrls()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            RdName = "Some.Release",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Bezzad,
            RdFiles = JsonSerializer.Serialize(new List<DebridClientFile>
            {
                new()
                {
                    Id = 0,
                    Path = "a.mkv",
                    Selected = true
                }
            })
        };

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("highway://4213/torrent/zip", result[0].RestrictedLink);
        Assert.Equal("Some.Release.zip", result[0].FileName);
    }

    [Fact]
    public async Task GetDownloadInfos_NonSymlink_FallsBackToZipSentinel_WhenOnlySomeFilesHaveDownloadUrls()
    {
        var files = new List<DebridClientFile>
        {
            new()
            {
                Id = 0,
                Path = "a.mkv",
                Selected = true,
                DownloadLink = "https://nl.dwld.link/dav/TORRENT/Some.Release/a.mkv?sig=1"
            },
            new()
            {
                Id = 1,
                Path = "b.nfo",
                Selected = true,
                DownloadLink = null
            }
        };

        var torrent = new Torrent
        {
            RdId = "4213",
            RdName = "Some.Release",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Bezzad,
            RdFiles = JsonSerializer.Serialize(files)
        };

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(true);

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("highway://4213/torrent/zip", result[0].RestrictedLink);
    }

    [Fact]
    public async Task GetDownloadInfos_NonSymlink_ReturnsEmpty_WhenAllFilesFilteredOut()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            RdName = "Some.Release",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Bezzad,
            RdFiles = JsonSerializer.Serialize(new List<DebridClientFile>
            {
                new()
                {
                    Id = 0,
                    Path = "sample.mkv",
                    Selected = true
                }
            })
        };

        _fileFilterMock.Setup(m => m.IsDownloadable(torrent, It.IsAny<String>(), It.IsAny<Int64>())).Returns(false);

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DownloadClient.Symlink)]
    [InlineData(DownloadClient.Bezzad)]
    public async Task GetDownloadInfos_ReturnsNull_WhenFilesNotYetPopulated(DownloadClient downloadClient)
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent,
            DownloadClient = downloadClient
        };

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDownloadInfos_ReturnsNull_ForSpoolReference()
    {
        var torrent = new Torrent
        {
            RdId = "3f9a1c2b4d5e6f708192a3b4c5d6e7f8",
            Type = DownloadType.Torrent,
            DownloadClient = DownloadClient.Bezzad
        };

        var result = await CreateClient().Object.GetDownloadInfos(torrent);

        Assert.Null(result);
    }

    [Fact]
    public async Task Unrestrict_ReturnsFreshSignedDownloadUrl()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("GetTransfer", ItExpr.Is<String>(s => s == "4213"), ItExpr.Is<String>(s => s == "torrent"))
                  .ReturnsAsync(new Transfer
                  {
                      Id = "4213",
                      DownloadUrl = "https://nl.dwld.link/dav/TORRENT/Some.Release/?download=zip"
                  });

        var result = await clientMock.Object.Unrestrict(torrent, "highway://4213/torrent/zip");

        Assert.Equal("https://nl.dwld.link/dav/TORRENT/Some.Release/?download=zip", result);
    }

    [Fact]
    public async Task Unrestrict_ReturnsSentinelUnchanged_ForPerFileSymlinkRef()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var result = await CreateClient().Object.Unrestrict(torrent, "highway://4213/torrent/2");

        Assert.Equal("highway://4213/torrent/2", result);
    }

    [Fact]
    public async Task Unrestrict_ReturnsRealUrlUnchanged_ForNonSentinelLink()
    {
        // Non-symlink per-file downloads embed the real, already-signed download_url directly
        // (see GetDownloadInfos) - nothing to resolve here, mirrors DebridLinkClient.
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        const String realUrl = "https://nl.dwld.link/dav/TORRENT/Some.Release/a.mkv?zip_user_id=1&zip_expires=1&zip_signature=abc";

        var result = await CreateClient().Object.Unrestrict(torrent, realUrl);

        Assert.Equal(realUrl, result);
    }

    [Fact]
    public async Task Unrestrict_Throws_WhenNoDownloadUrl()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("GetTransfer", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync(new Transfer
                  {
                      Id = "4213",
                      DownloadUrl = null
                  });

        await Assert.ThrowsAsync<Exception>(() => clientMock.Object.Unrestrict(torrent, "highway://4213/torrent/zip"));
    }

    [Fact]
    public async Task Unrestrict_ThrowsNotFoundMessage_WhenTransferIsGone()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Torrent
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("GetTransfer", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync((Transfer?) null);

        var ex = await Assert.ThrowsAsync<Exception>(() => clientMock.Object.Unrestrict(torrent, "highway://4213/torrent/zip"));
        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    public async Task Delete_OmitsService_ForSpoolId()
    {
        var torrent = new Torrent
        {
            RdId = "3f9a1c2b4d5e6f708192a3b4c5d6e7f8",
            Type = DownloadType.Torrent
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Boolean>>("DeleteTransfer", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync(true);

        await clientMock.Object.Delete(torrent);

        clientMock.Protected()
                  .Verify<Task<Boolean>>("DeleteTransfer", Times.Once(), ItExpr.Is<String>(s => s == torrent.RdId), ItExpr.IsNull<String>());
    }

    [Fact]
    public async Task Delete_PassesService_ForNumericId()
    {
        var torrent = new Torrent
        {
            RdId = "4213",
            Type = DownloadType.Nzb
        };

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Boolean>>("DeleteTransfer", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync(true);

        await clientMock.Object.Delete(torrent);

        clientMock.Protected()
                  .Verify<Task<Boolean>>("DeleteTransfer", Times.Once(), ItExpr.Is<String>(s => s == "4213"), ItExpr.Is<String>(s => s == "usenet"));
    }

    [Fact]
    public void GetSymlinkPath_PrependsServiceDirToApiFilePath()
    {
        // The real API returns files[].path already prefixed with the top-level folder name,
        // and content_path is "TORRENT/<name>" - the mount-relative path is "TORRENT/<name>/<file>".
        var torrent = new Torrent
        {
            Type = DownloadType.Torrent,
            RdName = "Some.Release",
            RdHost = "TORRENT/Some.Release",
            RdFiles = JsonSerializer.Serialize(new List<DebridClientFile>
            {
                new()
                {
                    Id = 0,
                    Path = "Some.Release/Some.Release.mkv",
                    Selected = true
                }
            })
        };

        var download = new Download
        {
            Link = "highway://4213/torrent/0",
            FileName = "Some.Release.mkv"
        };

        var path = HighwayDebridClient.GetSymlinkPath(torrent, download);

        Assert.Equal("TORRENT/Some.Release/Some.Release.mkv", path!.Replace('\\', '/'));
    }

    [Fact]
    public void GetSymlinkPath_LeavesAlreadyMountRelativePathUntouched()
    {
        var torrent = new Torrent
        {
            Type = DownloadType.Torrent,
            RdHost = "TORRENT/Some.Release",
            RdFiles = JsonSerializer.Serialize(new List<DebridClientFile>
            {
                new()
                {
                    Id = 0,
                    Path = "TORRENT/Some.Release/Some.Release.mkv",
                    Selected = true
                }
            })
        };

        var download = new Download
        {
            Link = "highway://4213/torrent/0",
            FileName = "Some.Release.mkv"
        };

        var path = HighwayDebridClient.GetSymlinkPath(torrent, download);

        Assert.Equal("TORRENT/Some.Release/Some.Release.mkv", path!.Replace('\\', '/'));
    }

    [Fact]
    public async Task GetUser_UsesUsernameFromStats()
    {
        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Stats?>>("GetStats")
                  .ReturnsAsync(new Stats
                  {
                      Username = "maxmuster",
                      Plan = "Pro",
                      Premium = true,
                      PlanExpiresAt = 1761955200
                  });

        var user = await clientMock.Object.GetUser();

        Assert.Equal("maxmuster", user.Username);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1761955200), user.Expiration);
    }

    [Fact]
    public async Task GetUser_LeavesExpirationNull_WhenPlanHasNoExpiry()
    {
        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Stats?>>("GetStats")
                  .ReturnsAsync(new Stats
                  {
                      Username = "maxmuster",
                      PlanExpiresAt = null
                  });

        var user = await clientMock.Object.GetUser();

        Assert.Null(user.Expiration);
    }

    [Fact]
    public async Task GetUser_FallsBackToPlan_WhenNoUsername()
    {
        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<Stats?>>("GetStats")
                  .ReturnsAsync(new Stats
                  {
                      Plan = "Pro"
                  });

        var user = await clientMock.Object.GetUser();

        Assert.Equal("Pro", user.Username);
    }

    [Fact]
    public async Task GetUser_FallsBackToStub_WhenStatsUnavailable()
    {
        var clientMock = CreateClient();
        clientMock.Protected().Setup<Task<Stats?>>("GetStats").ReturnsAsync((Stats?) null);

        var user = await clientMock.Object.GetUser();

        Assert.Equal("HIGHWAY", user.Username);
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsFiles_WhenCachedAndLinked()
    {
        const String hash = "3641ad15ad451518f558d5ce5b76be4a618911c1";

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<CheckResponse?>>("CheckCache", ItExpr.Is<String>(h => h == hash), ItExpr.Is<String>(s => s == "torrent"))
                  .ReturnsAsync(new CheckResponse
                  {
                      Service = "torrent",
                      Results = new()
                      {
                          [hash] = new()
                          {
                              Known = true,
                              Ready = true,
                              Id = "4213"
                          }
                      }
                  });

        clientMock.Protected()
                  .Setup<Task<Transfer?>>("GetTransfer", ItExpr.Is<String>(i => i == "4213"), ItExpr.IsAny<String>())
                  .ReturnsAsync(new Transfer
                  {
                      Id = "4213",
                      Files =
                      [
                          new()
                          {
                              Path = "Some.Release/Some.Release.mkv",
                              Size = 8120
                          },
                          new()
                          {
                              Path = "Some.Release/info.nfo",
                              Size = 42
                          }
                      ]
                  });

        var result = await clientMock.Object.GetAvailableFiles(hash);

        Assert.Equal(2, result.Count);
        Assert.Equal("Some.Release/Some.Release.mkv", result[0].Filename);
        Assert.Equal(8120, result[0].Filesize);
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsEmpty_WhenNotReady()
    {
        const String hash = "3641ad15ad451518f558d5ce5b76be4a618911c1";

        var clientMock = CreateClient();

        clientMock.Protected()
                  .Setup<Task<CheckResponse?>>("CheckCache", ItExpr.IsAny<String>(), ItExpr.IsAny<String>())
                  .ReturnsAsync(new CheckResponse
                  {
                      Service = "torrent",
                      Results = new()
                      {
                          [hash] = new()
                          {
                              Known = true,
                              Ready = false
                          }
                      }
                  });

        Assert.Empty(await clientMock.Object.GetAvailableFiles(hash));
    }

    [Fact]
    public async Task GetAvailableFiles_ReturnsEmpty_ForNonHexHash()
    {
        Assert.Empty(await CreateClient().Object.GetAvailableFiles("not-a-hash"));
    }
}
