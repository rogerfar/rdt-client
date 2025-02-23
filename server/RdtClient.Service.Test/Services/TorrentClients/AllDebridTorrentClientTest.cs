using AllDebridNET;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services.TorrentClients;
using File = AllDebridNET.File;

namespace RdtClient.Service.Test.Services.TorrentClients;

public class AllDebridTorrentClientTest
{
    private static readonly Magnet Magnet1HalfDownloaded = new()
    {
        Id = 1,
        Filename = "some-files",
        Hash = "hash-1",
        Status = "Downloading",
        StatusCode = 1,
        Downloaded = 50,
        Size = 100,
        Uploaded = 0,
        Seeders = 1,
        DownloadSpeed = 5,
        UploadSpeed = 0,
        UploadDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).Second,
        CompletionDate = 0
    };

    private static readonly Magnet Magnet1Finished = new()
    {
        Id = Magnet1HalfDownloaded.Id,
        Filename = Magnet1HalfDownloaded.Filename,
        Hash = Magnet1HalfDownloaded.Hash,
        Status = "Ready",
        StatusCode = 4,
        Size = Magnet1HalfDownloaded.Size,
        UploadDate = Magnet1HalfDownloaded.UploadDate,
        CompletionDate = new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).Second
    };

    private static readonly Magnet Magnet2QuarterDownloaded = new()
    {
        Id = 2,
        Filename = "some-other-files",
        Hash = "hash-2",
        Status = "Downloading",
        StatusCode = 1,
        Downloaded = 100,
        Size = 400,
        Uploaded = 0,
        Seeders = 1,
        DownloadSpeed = 5,
        UploadSpeed = 0,
        UploadDate = new DateTimeOffset(2020, 1, 1, 0, 5, 0, TimeSpan.Zero).Second,
        CompletionDate = 0
    };

    private static readonly Magnet Magnet2Finished = new()
    {
        Id = Magnet2QuarterDownloaded.Id,
        Filename = Magnet2QuarterDownloaded.Filename,
        Hash = Magnet2QuarterDownloaded.Hash,
        Status = "Ready",
        StatusCode = 4,
        Size = Magnet2QuarterDownloaded.Size,
        UploadDate = Magnet2QuarterDownloaded.UploadDate,
        CompletionDate = new DateTimeOffset(2020, 1, 1, 1, 5, 0, TimeSpan.Zero).Second
    };

    private static readonly List<File> IncludeExcludeFiles =
    [
        new()
        {
            FolderOrFileName = "include.txt",
            Size = 1,
            DownloadLink = "https://fake.url/include.txt"
        },
        new()
        {
            FolderOrFileName = "exclude.txt",
            Size = 1,
            DownloadLink = "https://fake.url/exclude.txt"
        },
        new()
        {
            FolderOrFileName = "include-folder",
            SubNodes =
            [
                new File
                {
                    FolderOrFileName = "include-folder-exclude-file.txt",
                    Size = 1,
                    DownloadLink = "https://fake.url/include-folder-exclude-file.txt"
                },
                new File
                {
                    FolderOrFileName = "include-folder-include-file.txt",
                    Size = 1,
                    DownloadLink = "https://fake.url/include-folder-include-file.txt"
                }
            ]
        },
        new()
        {
            FolderOrFileName = "exclude-folder",
            SubNodes =
            [
                new File
                {
                    FolderOrFileName = "exclude-folder-exclude-file.txt",
                    Size = 1,
                    DownloadLink = "https://fake.url/exclude-folder-exclude-file.txt"
                },
                new File
                {
                    FolderOrFileName = "exclude-folder-include-file.txt",
                    Size = 1,
                    DownloadLink = "https://fake.url/exclude-folder-include-file.txt"
                }
            ]
        }
    ];

    [Fact]
    public async Task GetTorrents_WhenFullSyncNoTorrents_ReturnsEmptyList()
    {
        // Arrange
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [],
                 Fullsync = true
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTorrents_WhenPartialSyncNoTorrents_ReturnsEmptyList()
    {
        // Arrange
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [],
                 Fullsync = true
             })
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 2,
                 Magnets = [],
                 Fullsync = false
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTorrents_WhenTorrentsFullSync_ReturnsOnlyTorrentsFromResponse()
    {
        // Arrange
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [Magnet1Finished],
                 Fullsync = true
             })
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 2,
                 Magnets = [Magnet2Finished],
                 Fullsync = true
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act

        // `GetTorrents` returns a reference to `_cache` if the `StatusLiveAsync` has `Fullsync = false`,
        // so when the second call `_cache.Add`s, it also affects `firstResult`.
        // `.ToList()` clones it so it won't be changed by the second call
        var firstResult = (await allDebridTorrentClient.GetTorrents()).ToList();
        var secondResult = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.Single(firstResult);
        Assert.Equal(Magnet1Finished.Id.ToString(), firstResult.First().Id);
        Assert.Single(secondResult);
        Assert.Equal(Magnet2Finished.Id.ToString(), secondResult.First().Id);
    }

    [Fact]
    public async Task GetTorrents_WhenPartialSyncResponseHasNewTorrent_ReturnsCachedAndNewTorrents()
    {
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [Magnet1Finished],
                 Fullsync = true
             })
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 2,
                 Magnets = [Magnet2Finished],
                 Fullsync = false
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act

        // `GetTorrents` returns a reference to `_cache` if the `StatusLiveAsync` has `Fullsync = false`,
        // so when the second call `_cache.Add`s, it also affects `firstResult`.
        // `.ToList()` clones it so it won't be changed by the second call
        var firstResult = (await allDebridTorrentClient.GetTorrents()).ToList();
        var secondResult = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.Single(firstResult);
        Assert.Equal(Magnet1Finished.Id.ToString(), firstResult[0].Id);
        Assert.Equal(2, secondResult.Count);
        Assert.Contains(secondResult, t => t.Id == Magnet1Finished.Id.ToString());
        Assert.Contains(secondResult, t => t.Id == Magnet2Finished.Id.ToString());
    }

    [Fact]
    public async Task GetTorrents_WhenPartialSyncResponseHasUpdate_ReturnsCachedAndUpdatedTorrents()
    {
        // Double check the fakes are as we expect
        Assert.Equal(Magnet1Finished.Id, Magnet1HalfDownloaded.Id);
        Assert.NotEqual(Magnet1Finished.Status, Magnet1HalfDownloaded.Status);

        // Arrange
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [Magnet1HalfDownloaded, Magnet2Finished],
                 Fullsync = true
             })
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 2,
                 Magnets = [Magnet1Finished],
                 Fullsync = false
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act

        // `GetTorrents` returns a reference to `_cache` if the `StatusLiveAsync` has `Fullsync = false`,
        // so when the second call `_cache.Add`s, it also affects `firstResult`.
        // `.ToList()` clones it so it won't be changed by the second call
        var firstResult = (await allDebridTorrentClient.GetTorrents()).ToList();
        var secondResult = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.Equal(2, firstResult.Count);
        Assert.Contains(firstResult, t => t.Id == Magnet1HalfDownloaded.Id.ToString() && t.Status == Magnet1HalfDownloaded.Status);
        Assert.Contains(firstResult, t => t.Id == Magnet2Finished.Id.ToString());
        Assert.Equal(2, secondResult.Count);
        Assert.Contains(secondResult, t => t.Id == Magnet1Finished.Id.ToString() && t.Status == Magnet1Finished.Status);
        Assert.Contains(secondResult, t => t.Id == Magnet2Finished.Id.ToString());
    }

    public static IEnumerable<Object[]> DownloadingMagnetsWithProgress()
    {
        return [[Magnet1HalfDownloaded, 50], [Magnet2QuarterDownloaded, 25]];
    }

    [Theory]
    [MemberData(nameof(DownloadingMagnetsWithProgress))]
    public async Task Map_WhenTorrentDownloading_ComputesProgress(Magnet magnet, Int64 expectedProgress)
    {
        // Arrange
        var mocks = new Mocks();

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [magnet],
                 Fullsync = true
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act

        // We have to use `GetTorrents` since `Map` is private
        var result = await allDebridTorrentClient.GetTorrents();

        // Assert
        Assert.Equal(expectedProgress, result.First().Progress);
    }

    [Fact]
    public async Task UpdateData_WhenTorrentRdIdNull_ReturnsUnmodifiedTorrent()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = null,
            Hash = "hash",
            RdName = "rdName"
        };

        var serializedOriginal = JsonConvert.SerializeObject(torrent);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.UpdateData(torrent, null);

        // Assert
        Assert.Equal(serializedOriginal, JsonConvert.SerializeObject(result));
        mocks.AllDebridClientFactoryMock.Verify(f => f.GetClient(), Times.Never);
    }

    [Fact]
    public async Task UpdateData_WhenTorrentClientTorrentNotProvided_FetchesFromAPIAndUpdatesTorrentAccordingly()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = Magnet1Finished.Id.ToString(),
            Hash = Magnet1Finished.Hash!,
            RdName = null,
            RdSize = null,
            RdStatus = null
        };

        mocks.AllDebridClientMock.Setup(c => c.Magnet.StatusAsync(torrent.RdId, It.IsAny<CancellationToken>())).ReturnsAsync(Magnet1Finished);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.UpdateData(torrent, null);

        // Assert
        mocks.AllDebridClientMock.Verify(c => c.Magnet.StatusAsync(torrent.RdId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(Magnet1Finished.Filename, result.RdName);
        Assert.Equal(Magnet1Finished.Size, result.RdSize);
        Assert.Equal(Provider.AllDebrid, result.ClientKind);
        Assert.Equal(TorrentStatus.Finished, result.RdStatus);

        // It mutates the original object:
        Assert.Equal(torrent, result);
    }

    [Fact]
    public async Task UpdateData_WhenTorrentClientTorrentNotProvidedAndTorrentDeletedFromAD_UpdatesRdStatusRaw()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "123456",
            Hash = "fake-hash-123456",
            RdStatus = null,
            RdStatusRaw = null
        };

        mocks.AllDebridClientMock.Setup(c => c.Magnet.StatusAsync(torrent.RdId, It.IsAny<CancellationToken>()))
             .ThrowsAsync(new AllDebridException("Magnet not found", "MAGNET_INVALID_ID"));

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.UpdateData(torrent, null);

        // Assert
        Assert.Equal("deleted", result.RdStatusRaw);
        Assert.Null(result.RdStatus);

        // It mutates the original object:
        Assert.Equal(torrent, result);
    }

    [Fact]
    public async Task UpdateData_WhenTorrentClientTorrentIsProvided_DoesNotFetchFromApiAndUpdatesTorrentAccordingly()
    {
        // Double check fakes are as we expect
        Assert.Equal(4, Magnet1Finished.StatusCode);

        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = Magnet1Finished.Id.ToString()
        };

        mocks.AllDebridClientMock.SetupSequence(c => c.Magnet.StatusLiveAsync(It.IsAny<Int64>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MagnetStatusLiveResponse
             {
                 Counter = 1,
                 Magnets = [Magnet2Finished],
                 Fullsync = true
             });

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);
        var torrentClientTorrent = (await allDebridTorrentClient.GetTorrents()).First();

        // Act
        var result = await allDebridTorrentClient.UpdateData(torrent, torrentClientTorrent);

        // Assert
        Assert.Equal(torrentClientTorrent.Filename, torrent.RdName);
        Assert.Equal(torrentClientTorrent.Bytes, result.RdSize);
        Assert.Equal(TorrentStatus.Finished, torrent.RdStatus);
        Assert.Equal(Provider.AllDebrid, torrent.ClientKind);

        // It mutates the original object:
        Assert.Equal(torrent, result);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenTorrentRdIdNull_ReturnsNull()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = null
        };

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.Null(result);
        mocks.AllDebridClientFactoryMock.Verify(f => f.GetClient(), Times.Never);
    }

    [Fact]
    public async Task GetDownloadLinks_ByDefault_GetsLinksForAllFiles()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1"
        };

        List<File> files =
        [
            new()
            {
                FolderOrFileName = "file-1.txt",
                Size = 50,
                DownloadLink = "https://fake.url/file-1.txt"
            },
            new()
            {
                FolderOrFileName = "file-2.txt",
                Size = 150,
                DownloadLink = "https://fake.url/file-2.txt"
            }
        ];

        var expectedLinksSet = new HashSet<String>(files.Select(n => n.DownloadLink)!);
        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(Int64.Parse(torrent.RdId), It.IsAny<CancellationToken>())).ReturnsAsync(files);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.NotNull(result);
        var linksSet = new HashSet<String>(result);
        Assert.Equal(expectedLinksSet, linksSet);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenDownloadMinSizeSet_GetsLinksForOnlyFilesAboveThatSize()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",

            // NB: this is in MB, the file sizes below are in B
            DownloadMinSize = 100
        };

        List<File> files =
        [
            new()
            {
                FolderOrFileName = "too-small.txt",
                Size = (torrent.DownloadMinSize - 1) * 1024 * 1024,
                DownloadLink = "https://fake.url/too-small.txt"
            },
            new()
            {
                FolderOrFileName = "bigger-than-min.txt",
                Size = (torrent.DownloadMinSize + 1) * 1024 * 1024,
                DownloadLink = "https://fake.url/bigger-than-min.txt"
            }
        ];

        var expectedLinksSet = new HashSet<String>(files.Where(m => m.Size > torrent.DownloadMinSize * 1024 * 1024).Select(n => n.DownloadLink)!);
        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(Int64.Parse(torrent.RdId), It.IsAny<CancellationToken>())).ReturnsAsync(files);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.NotNull(result);
        var linksSet = new HashSet<String>(result);
        Assert.Equal(expectedLinksSet, linksSet);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenIncludeRegexSet_GetsLinksForOnlyFilesMatchingRegex()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = "include"
        };

        var expectedLinksSet = new HashSet<String>([
            "https://fake.url/include.txt", "https://fake.url/include-folder-exclude-file.txt", "https://fake.url/include-folder-include-file.txt",
            "https://fake.url/exclude-folder-include-file.txt"
        ]);

        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(Int64.Parse(torrent.RdId), It.IsAny<CancellationToken>())).ReturnsAsync(IncludeExcludeFiles);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.NotNull(result);
        var linksSet = new HashSet<String>(result);
        Assert.Equal(expectedLinksSet, linksSet);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenExcludeRegexSet_GetsLinksForOnlyFilesNotMatchingRegex()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            ExcludeRegex = "exclude"
        };

        var expectedLinksSet = new HashSet<String>([
            "https://fake.url/include.txt", "https://fake.url/include-folder-include-file.txt"
        ]);

        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(Int64.Parse(torrent.RdId), It.IsAny<CancellationToken>())).ReturnsAsync(IncludeExcludeFiles);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.NotNull(result);
        var linksSet = new HashSet<String>(result);
        Assert.Equal(expectedLinksSet, linksSet);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenIncludeAndExcludeRegexSet_GetsLinksForOnlyFilesMatchingIncludeRegexEvenIfNotMatchingExcludeRegex()
    {
        // Arrange
        var mocks = new Mocks();

        var torrentInclude = new Torrent
        {
            RdId = "1",
            IncludeRegex = "include"
        };

        var torrentIncludeExclude = new Torrent
        {
            RdId = "2",
            IncludeRegex = "include",
            ExcludeRegex = "exclude"
        };

        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(IncludeExcludeFiles);

        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var includeOnlyResult = await allDebridTorrentClient.GetDownloadLinks(torrentInclude);
        var includeExcludeResult = await allDebridTorrentClient.GetDownloadLinks(torrentIncludeExclude);

        // Assert
        Assert.NotNull(includeOnlyResult);
        Assert.NotNull(includeExcludeResult);
        var includeOnlyLinksSet = new HashSet<String>(includeOnlyResult);
        var includeExcludeLinksSet = new HashSet<String>(includeExcludeResult);
        Assert.Equal(includeOnlyLinksSet, includeExcludeLinksSet);
    }

    [Fact]
    public async Task GetDownloadLinks_WhenAllFilesExcluded_ReturnsAllFiles()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",

            // NB: this is in MB, the file sizes below are in B
            DownloadMinSize = 100
        };

        List<File> files =
        [
            new()
            {
                FolderOrFileName = "too-small-1.txt",
                Size = (torrent.DownloadMinSize - 1) * 1024 * 1024,
                DownloadLink = "https://fake.url/too-small-1.txt"
            },
            new()
            {
                FolderOrFileName = "too-small-2.txt",
                Size = (torrent.DownloadMinSize - 1) * 1024 * 1024,
                DownloadLink = "https://fake.url/too-small-2.txt"
            }
        ];

        var expectedLinksSet = new HashSet<String>(files.Select(n => n.DownloadLink)!);
        mocks.AllDebridClientMock.Setup(c => c.Magnet.FilesAsync(Int64.Parse(torrent.RdId), It.IsAny<CancellationToken>())).ReturnsAsync(files);
        var allDebridTorrentClient = new AllDebridTorrentClient(mocks.LoggerMock.Object, mocks.AllDebridClientFactoryMock.Object);

        // Act
        var result = await allDebridTorrentClient.GetDownloadLinks(torrent);

        // Assert
        Assert.NotNull(result);
        var linksSet = new HashSet<String>(result);
        Assert.Equal(expectedLinksSet, linksSet);
    }

    private class Mocks
    {
        public readonly Mock<IAllDebridNetClientFactory> AllDebridClientFactoryMock;
        public readonly Mock<IAllDebridNETClient> AllDebridClientMock;
        public readonly Mock<ILogger<AllDebridTorrentClient>> LoggerMock;

        public Mocks()
        {
            LoggerMock = new();
            AllDebridClientMock = new();
            AllDebridClientFactoryMock = new();
            AllDebridClientFactoryMock.Setup(f => f.GetClient()).Returns(AllDebridClientMock.Object);
        }
    }
}
