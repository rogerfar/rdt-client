using Moq;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;
using Synology.Api.Client;
using Synology.Api.Client.Apis.DownloadStation;
using Synology.Api.Client.Apis.DownloadStation.Task.Models;

namespace RdtClient.Service.Test.Services.Downloaders;

class Mocks
{
    public readonly String Gid;
    public readonly Mock<ISynologyClient> SynologyClientMock = new();
    public readonly Mock<IDownloadStationTaskEndpoint> TaskEndpointMock = new();

    public Mocks(String gid = "123456")
    {
        Gid = gid;
        var downloadStationApiMock = new Mock<IDownloadStationApi>();
        downloadStationApiMock.Setup(a => a.TaskEndpoint()).Returns(TaskEndpointMock.Object);
        SynologyClientMock.Setup(c => c.DownloadStationApi()).Returns(downloadStationApiMock.Object);
    }
}

class FakeDelayProvider : IDelayProvider
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
    public async Task Download_WhenAlreadyAdded_Throws()
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
        var exception = await Assert.ThrowsAsync<Exception>(downloadStationDownloader.Download);

        // Assert
        Assert.Contains("already been added", exception.Message, StringComparison.OrdinalIgnoreCase);
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
}
