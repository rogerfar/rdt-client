using System.IO.Abstractions.TestingHelpers;
using Moq;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;
using Synology.Api.Client;
using Synology.Api.Client.ApiDescription;
using Synology.Api.Client.Apis.DownloadStation;
using Synology.Api.Client.Apis.DownloadStation.Task.Models;
using Synology.Api.Client.Apis.FileStation;
using Synology.Api.Client.Apis.FileStation.CreateFolder;
using Synology.Api.Client.Apis.FileStation.CreateFolder.Models;
using Synology.Api.Client.Exceptions;

namespace RdtClient.Service.Test.Services.Downloaders;

internal class Mocks
{
    public readonly Mock<IFileStationCreateFolderEndpoint> CreateFolderEndpointMock = new();
    public readonly String Gid;
    public readonly Mock<ISynologyClient> SynologyClientMock = new();
    public readonly Mock<IDownloadStationTaskEndpoint> TaskEndpointMock = new();

    public Mocks(String gid = "123456")
    {
        Gid = gid;

        var downloadStationApiMock = new Mock<IDownloadStationApi>();
        downloadStationApiMock.Setup(a => a.TaskEndpoint()).Returns(TaskEndpointMock.Object);
        SynologyClientMock.Setup(c => c.DownloadStationApi()).Returns(downloadStationApiMock.Object);

        var fileStationApiMock = new Mock<IFileStationApi>();
        fileStationApiMock.Setup(a => a.CreateFolderEndpoint()).Returns(CreateFolderEndpointMock.Object);
        SynologyClientMock.Setup(c => c.FileStationApi()).Returns(fileStationApiMock.Object);

        CreateFolderEndpointMock.Setup(e => e.CreateAsync(It.IsAny<String[]>(), It.IsAny<Boolean>()))
                                .ReturnsAsync(new FileStationCreateFolderCreateResponse());
    }
}

internal class FakeDelayProvider : IDelayProvider
{
    public Task Delay(Int32 milliseconds)
    {
        return Task.CompletedTask;
    }
}

public class DownloadStationDownloaderTest
{
    [Fact]
    public async Task Download_WhenRemotePathEmpty_Throws()
    {
        // Arrange
        var synologyClientMock = new Mock<ISynologyClient>();
        var gid = Guid.NewGuid();

        var downloadStationDownloader = new DownloadStationDownloader(gid.ToString(),
                                                                      "https://fake.url/file.txt",
                                                                      "",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      synologyClientMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(downloadStationDownloader.Download);

        // Assert
        Assert.Contains("invalid file path", exception.Message, StringComparison.OrdinalIgnoreCase);
        synologyClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_WhenTaskAlreadyExists_AdoptsItAndReturnsGid()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ReturnsAsync(new DownloadStationTask());

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/path/on/remote/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object);

        // Act
        var gid = await downloadStationDownloader.Download();

        // Assert: an existing task must be reused (idempotent), not re-created or thrown as "already added".
        // Throwing would brick every retry that reuses the gid because the DownloadStation delete fails to
        // deserialize its response, so the task is never actually removed.
        Assert.Equal(mocks.Gid, gid);
        mocks.TaskEndpointMock.Verify(t => t.GetInfoAsync(mocks.Gid), Times.Once);
        mocks.TaskEndpointMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_WhenAddedSuccessfully_ReturnsGid()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ThrowsAsync(new());

        mocks.TaskEndpointMock.Setup(t => t.ListAsync())
             .ReturnsAsync(new DownloadStationTaskListResponse
             {
                 Total = 0,
                 Offset = 0
             });

        mocks.TaskEndpointMock.Setup(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()))
             .ReturnsAsync(new DownloadStationTaskCreateResponse
             {
                 TaskId = [mocks.Gid]
             });

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/path/on/remote/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object);

        // Act
        var result = await downloadStationDownloader.Download();

        // Assert
        Assert.Equal(mocks.Gid, result);
        mocks.TaskEndpointMock.Verify(t => t.GetInfoAsync(mocks.Gid), Times.Once);
        mocks.TaskEndpointMock.Verify(t => t.ListAsync(), Times.Once);
        mocks.TaskEndpointMock.Verify(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()), Times.Once);

        mocks.TaskEndpointMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_CreatesDestinationFolderBeforeCreatingTask()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ThrowsAsync(new());
        mocks.TaskEndpointMock.Setup(t => t.ListAsync()).ReturnsAsync(new DownloadStationTaskListResponse { Total = 0, Offset = 0 });

        var calls = new List<String>();
        mocks.CreateFolderEndpointMock.Setup(e => e.CreateAsync(It.IsAny<String[]>(), It.IsAny<Boolean>()))
             .ReturnsAsync(new FileStationCreateFolderCreateResponse())
             .Callback(() => calls.Add("folder"));
        mocks.TaskEndpointMock.Setup(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()))
             .ReturnsAsync(new DownloadStationTaskCreateResponse { TaskId = [mocks.Gid] })
             .Callback(() => calls.Add("task"));

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/Media/Downloads/Torrents/MyTorrent/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object);

        // Act
        await downloadStationDownloader.Download();

        // Assert
        Assert.Equal(["folder", "task"], calls);
        mocks.CreateFolderEndpointMock.Verify(e => e.CreateAsync(It.Is<String[]>(p => p.Single() == "/Media/Downloads/Torrents/MyTorrent"), true), Times.Once);
    }

    [Fact]
    public async Task Download_WhenSynologyReportsSessionError119_ReAuthenticatesAndRetries()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ThrowsAsync(new());
        mocks.TaskEndpointMock.Setup(t => t.ListAsync()).ReturnsAsync(new DownloadStationTaskListResponse { Total = 0, Offset = 0 });
        mocks.TaskEndpointMock.Setup(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()))
             .ReturnsAsync(new DownloadStationTaskCreateResponse { TaskId = [mocks.Gid] });

        // The first folder-create fails with DSM error 119 ("SID not found"); after re-auth the retry succeeds.
        mocks.CreateFolderEndpointMock.SetupSequence(e => e.CreateAsync(It.IsAny<String[]>(), It.IsAny<Boolean>()))
             .ThrowsAsync(new SynologyApiException(new Mock<IApiInfo>().Object, "FileStation.CreateFolder", 119, "SID not found"))
             .ReturnsAsync(new FileStationCreateFolderCreateResponse());

        var reacquired = 0;

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/Media/Downloads/Torrents/MyTorrent/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object,
                                                                      reacquireClient: () =>
                                                                      {
                                                                          reacquired++;

                                                                          return Task.FromResult(mocks.SynologyClientMock.Object);
                                                                      });

        // Act
        var result = await downloadStationDownloader.Download();

        // Assert
        Assert.Equal(mocks.Gid, result);
        Assert.Equal(1, reacquired);
        mocks.CreateFolderEndpointMock.Verify(e => e.CreateAsync(It.IsAny<String[]>(), It.IsAny<Boolean>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Download_After5Tries_Throws()
    {
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ThrowsAsync(new());

        var emptyListResponse = new DownloadStationTaskListResponse
        {
            Total = 0,
            Offset = 0
        };

        mocks.TaskEndpointMock.SetupSequence(t => t.ListAsync())
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse);

        mocks.TaskEndpointMock.SetupSequence(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()))
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ThrowsAsync(new());

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/path/on/remote/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object,
                                                                      new FakeDelayProvider());

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(downloadStationDownloader.Download);

        // Assert
        Assert.Contains("unable to download", exception.Message, StringComparison.OrdinalIgnoreCase);
        mocks.TaskEndpointMock.Verify(t => t.GetInfoAsync(mocks.Gid), Times.Once);
        mocks.TaskEndpointMock.Verify(t => t.ListAsync(), Times.Exactly(5));
        mocks.TaskEndpointMock.Verify(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()), Times.Exactly(5));

        mocks.TaskEndpointMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_WhenSuccessfulAfter4Tries_ReturnsGid()
    {
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ThrowsAsync(new());

        var emptyListResponse = new DownloadStationTaskListResponse
        {
            Total = 0,
            Offset = 0
        };

        mocks.TaskEndpointMock.SetupSequence(t => t.ListAsync())
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse)
             .ReturnsAsync(emptyListResponse);

        mocks.TaskEndpointMock.SetupSequence(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()))
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ThrowsAsync(new())
             .ReturnsAsync(new DownloadStationTaskCreateResponse
             {
                 TaskId = [mocks.Gid]
             });

        var downloadStationDownloader = new DownloadStationDownloader(mocks.Gid,
                                                                      "https://fake.url/file.txt",
                                                                      "/path/on/remote/file.txt",
                                                                      "/path/to/file.txt",
                                                                      "download-path",
                                                                      mocks.SynologyClientMock.Object,
                                                                      new FakeDelayProvider());

        // Act
        var result = await downloadStationDownloader.Download();

        // Assert
        Assert.Equal(mocks.Gid, result);
        mocks.TaskEndpointMock.Verify(t => t.GetInfoAsync(mocks.Gid), Times.Once);
        mocks.TaskEndpointMock.Verify(t => t.ListAsync(), Times.Exactly(5));
        mocks.TaskEndpointMock.Verify(t => t.CreateAsync(It.IsAny<DownloadStationTaskCreateRequest>()), Times.Exactly(5));

        mocks.TaskEndpointMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(DownloadStationTaskStatus.Finished)]
    [InlineData(DownloadStationTaskStatus.Downloaded)]
    [InlineData(DownloadStationTaskStatus.Seeding)]
    public async Task Update_WhenCompleteAndFileExists_CompletesWithoutError(DownloadStationTaskStatus status)
    {
        // Arrange
        var mocks = new Mocks();
        const String filePath = "/data/downloads/file.mkv";

        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ReturnsAsync(new DownloadStationTask { Status = status });

        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(filePath, new MockFileData("content"));

        var downloader = new DownloadStationDownloader(mocks.Gid, "https://fake.url/file.mkv", "/remote/file.mkv", filePath, "download-path",
                                                       mocks.SynologyClientMock.Object, new FakeDelayProvider(), fileSystem);

        DownloadCompleteEventArgs? completed = null;
        downloader.DownloadComplete += (_, e) => completed = e;

        // Act
        await downloader.Update();

        // Assert
        Assert.NotNull(completed);
        Assert.Null(completed!.Error);
    }

    [Fact]
    public async Task Update_WhenFinishedButFileMissing_CompletesWithError()
    {
        // Arrange
        var mocks = new Mocks();
        const String filePath = "/data/downloads/file.mkv";

        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ReturnsAsync(new DownloadStationTask { Status = DownloadStationTaskStatus.Finished });

        var downloader = new DownloadStationDownloader(mocks.Gid, "https://fake.url/file.mkv", "/remote/file.mkv", filePath, "download-path",
                                                       mocks.SynologyClientMock.Object, new FakeDelayProvider(), new MockFileSystem());

        DownloadCompleteEventArgs? completed = null;
        downloader.DownloadComplete += (_, e) => completed = e;

        // Act
        await downloader.Update();

        // Assert
        Assert.NotNull(completed);
        Assert.NotNull(completed!.Error);
        Assert.Contains("no file was found", completed.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WhenErrorDetailSet_CancelsAndReportsError()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid))
             .ReturnsAsync(new DownloadStationTask
             {
                 Status = DownloadStationTaskStatus.Downloading,
                 StatusExtra = new DownloadStationTaskStatusExtra { ErrorDetail = "torrent_duplicate" }
             });
        mocks.TaskEndpointMock.Setup(t => t.DeleteAsync(It.IsAny<DownloadStationTaskDeleteRequest>()))
             .ReturnsAsync(new DownloadStationTaskDeleteResponse());

        var downloader = new DownloadStationDownloader(mocks.Gid, "https://fake.url/file.mkv", "/remote/file.mkv", "/data/downloads/file.mkv", "download-path",
                                                       mocks.SynologyClientMock.Object, new FakeDelayProvider(), new MockFileSystem());

        DownloadCompleteEventArgs? completed = null;
        downloader.DownloadComplete += (_, e) => completed = e;

        // Act
        await downloader.Update();

        // Assert
        Assert.NotNull(completed);
        Assert.Contains("torrent_duplicate", completed!.Error!, StringComparison.OrdinalIgnoreCase);
        mocks.TaskEndpointMock.Verify(t => t.DeleteAsync(It.IsAny<DownloadStationTaskDeleteRequest>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenCaptchaNeeded_ReportsError()
    {
        // Arrange
        var mocks = new Mocks();
        mocks.TaskEndpointMock.Setup(t => t.GetInfoAsync(mocks.Gid)).ReturnsAsync(new DownloadStationTask { Status = DownloadStationTaskStatus.CaptchaNeeded });
        mocks.TaskEndpointMock.Setup(t => t.DeleteAsync(It.IsAny<DownloadStationTaskDeleteRequest>())).ReturnsAsync(new DownloadStationTaskDeleteResponse());

        var downloader = new DownloadStationDownloader(mocks.Gid, "https://fake.url/file.mkv", "/remote/file.mkv", "/data/downloads/file.mkv", "download-path",
                                                       mocks.SynologyClientMock.Object, new FakeDelayProvider(), new MockFileSystem());

        DownloadCompleteEventArgs? completed = null;
        downloader.DownloadComplete += (_, e) => completed = e;

        // Act
        await downloader.Update();

        // Assert
        Assert.NotNull(completed);
        Assert.Contains("captcha", completed!.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
