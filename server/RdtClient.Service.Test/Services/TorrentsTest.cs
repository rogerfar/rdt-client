using System.Diagnostics;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;
using RdtClient.Service.Wrappers;
using TorrentsService = RdtClient.Service.Services.Torrents;

namespace RdtClient.Service.Test.Services;

class Mocks
{
    public readonly Mock<IProcessFactory> ProcessFactoryMock;
    public readonly Mock<IProcess> ProcessMock;
    public readonly Mock<ILogger<TorrentsService>> TorrentsLoggerMock;
    public readonly Mock<IDownloads> DownloadsMock;
    public readonly Mock<ITorrentData> TorrentDataMock;

    public Mocks()
    {
        TorrentDataMock = new();
        DownloadsMock = new();

        TorrentsLoggerMock = new();

        ProcessMock = new();
        ProcessStartInfo startInfo = new();
        ProcessMock.SetupProperty(p => p.StartInfo, startInfo);
        ProcessFactoryMock = new();
        ProcessFactoryMock.Setup(p => p.NewProcess()).Returns(ProcessMock.Object);
    }
}

public class TorrentsTest
{
    public static TheoryData<Torrent, List<Download>> TorrentAndDownload()
    {
        var torrent = new Torrent()
        {
            RdName = "TestTorrent",
            Hash = "123ABC",
            Category = "Movies",
            RdSize = 100,
            TorrentId = Guid.Empty
        };

        List<Download> downloads =
        [
            new()
            {
                FileName = "file.txt",
                TorrentId = torrent.TorrentId
            }
        ];

        return new ()
        {
            {
              torrent,
              downloads
            }
        };
    }

    [Theory]
    [MemberData(nameof(TorrentAndDownload))]
    public async Task RunTorrentComplete_WhenCommandSet_ShouldRunCommand(Torrent torrent, List<Download> downloads)
    {
        // Arrange
        var settings = new DbSettings
        {
            General = new()
            {
                RunOnTorrentCompleteFileName = "/bin/echo",
                RunOnTorrentCompleteArguments = "%N %L %F %R %D %C %Z %I"
            }
        };

        var mocks = new Mocks();

        mocks.TorrentDataMock.Setup(t => t.GetById(torrent.TorrentId)).Returns(Task.FromResult<Torrent?>(torrent));
        mocks.DownloadsMock.Setup(d => d.GetForTorrent(torrent.TorrentId)).ReturnsAsync(downloads);

        var downloadPath = $"{settings.DownloadClient.DownloadPath}/{torrent.Category}";
        var torrentPath = $"{downloadPath}/{torrent.RdName}";
        var filePath = $"{torrentPath}/{downloads[0].FileName}";

        var fileSystemMock = new MockFileSystem(new Dictionary<String, MockFileData>
        {
            {
                filePath, new("Test file")
            },
        });

        var torrents = new TorrentsService(mocks.TorrentsLoggerMock.Object,
                                           mocks.TorrentDataMock.Object,
                                           mocks.DownloadsMock.Object,
                                           mocks.ProcessFactoryMock.Object,
                                           fileSystemMock,
                                           null!, // Torrent Clients are not used by `RunTorrentComplete`, this is fine
                                           null!,
                                           null!,
                                           null!,
                                           null!);

        mocks.ProcessMock.Setup(p => p.WaitForExit(It.IsAny<Int32>())).Returns(true);

        // Act
        await torrents.RunTorrentComplete(torrent.TorrentId, settings);

        // Assert
        Assert.Equal("/bin/echo", mocks.ProcessMock.Object.StartInfo.FileName);

        var expectedArgumentsSb = new StringBuilder();
        expectedArgumentsSb.Append($"\"{torrent.RdName}\"");
        expectedArgumentsSb.Append($" \"{torrent.Category}\"");
        expectedArgumentsSb.Append($" \"{filePath}\"");
        expectedArgumentsSb.Append($" \"{downloadPath}\"");
        expectedArgumentsSb.Append($" \"{torrentPath}\"");
        expectedArgumentsSb.Append($" {downloads.Count.ToString()}");
        expectedArgumentsSb.Append($" {torrent.RdSize.ToString()}");
        expectedArgumentsSb.Append($" {torrent.Hash}");
        Assert.Equal(expectedArgumentsSb.ToString(), mocks.ProcessMock.Object.StartInfo.Arguments);

        mocks.ProcessMock.Verify(p => p.Start(), Times.Once);
    }

    [Theory]
    [MemberData(nameof(TorrentAndDownload))]
    public async Task RunTorrentComplete_WhenCommandNotSet_ShouldNotRunCommand(Torrent torrent, List<Download> downloads)
    {
        // Arrange
        var settings = new DbSettings()
        {
            General = new()
            {
                RunOnTorrentCompleteFileName = null
            }
        };

        var mocks = new Mocks();

        mocks.TorrentDataMock.Setup(t => t.GetById(torrent.TorrentId)).Returns(Task.FromResult<Torrent?>(torrent));
        mocks.DownloadsMock.Setup(d => d.GetForTorrent(torrent.TorrentId)).ReturnsAsync(downloads);

        var downloadPath = $"{settings.DownloadClient.DownloadPath}/{torrent.Category}";
        var torrentPath = $"{downloadPath}/{torrent.RdName}";
        var filePath = $"{torrentPath}/{downloads[0].FileName}";

        var fileSystemMock = new MockFileSystem(new Dictionary<String, MockFileData>
        {
            {
                filePath, new("Test file")
            },
        });

        var torrents = new TorrentsService(mocks.TorrentsLoggerMock.Object,
                                           mocks.TorrentDataMock.Object,
                                           mocks.DownloadsMock.Object,
                                           mocks.ProcessFactoryMock.Object,
                                           fileSystemMock,
                                           null!, // Torrent Clients are not used by `RunTorrentComplete`, this is fine
                                           null!,
                                           null!,
                                           null!,
                                           null!);

        //Act
        await torrents.RunTorrentComplete(torrent.TorrentId, settings);

        //Assert
        mocks.ProcessFactoryMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(TorrentAndDownload))]
    public async Task RunTorrentComplete_WhenStdOut_Logs(Torrent torrent, List<Download> downloads)
    {
        // Arrange
        var settings = new DbSettings()
        {
            General = new()
            {
                RunOnTorrentCompleteFileName = "/bin/echo"
            }
        };

        var mocks = new Mocks();

        mocks.TorrentDataMock.Setup(t => t.GetById(torrent.TorrentId)).Returns(Task.FromResult<Torrent?>(torrent));
        mocks.DownloadsMock.Setup(d => d.GetForTorrent(torrent.TorrentId)).ReturnsAsync(downloads);

        var downloadPath = $"{settings.DownloadClient.DownloadPath}/{torrent.Category}";
        var torrentPath = $"{downloadPath}/{torrent.RdName}";
        var filePath = $"{torrentPath}/{downloads[0].FileName}";

        var fileSystemMock = new MockFileSystem(new Dictionary<String, MockFileData>
        {
            {
                filePath, new("Test file")
            },
        });

        var torrents = new TorrentsService(mocks.TorrentsLoggerMock.Object,
                                           mocks.TorrentDataMock.Object,
                                           mocks.DownloadsMock.Object,
                                           mocks.ProcessFactoryMock.Object,
                                           fileSystemMock,
                                           null!, // Torrent Clients are not used by `RunTorrentComplete`, this is fine
                                           null!,
                                           null!,
                                           null!,
                                           null!);

        mocks.ProcessMock.Setup(p => p.WaitForExit(It.IsAny<Int32>()))
             .Callback(() =>
             {
                 mocks.ProcessMock.Raise(m => m.OutputDataReceived += null, this, "output-line 1");
                 mocks.ProcessMock.Raise(m => m.OutputDataReceived += null, this, "output-line 2");
                 mocks.ProcessMock.Raise(m => m.OutputDataReceived += null, this, "output-line 3");
             })
             .Returns(true);

        // Act
        await torrents.RunTorrentComplete(torrent.TorrentId, settings);

        // Assert
        mocks.ProcessMock.Verify(p => p.BeginOutputReadLine(), Times.Once);

        var messages = mocks.TorrentsLoggerMock.Invocations.Where(i => i.Method.Name == "Log").Select(i => i.Arguments[2].ToString()).Where(m => m != null).ToList();
        var exitedWithOutputMessages = messages.Where(m => Regex.IsMatch(m!, "exited with output")).ToList();
        Assert.NotNull(exitedWithOutputMessages);
        Assert.Single(exitedWithOutputMessages);
        var exitedWithOutputMessage = exitedWithOutputMessages.First();
        Assert.NotNull(exitedWithOutputMessage);
        Assert.Matches("output-line 1", exitedWithOutputMessage);
        Assert.Matches("output-line 2", exitedWithOutputMessage);
        Assert.Matches("output-line 3", exitedWithOutputMessage);
    }

    [Theory]
    [MemberData(nameof(TorrentAndDownload))]
    public async Task RunTorrentComplete_WhenStdErr_Logs(Torrent torrent, List<Download> downloads)
    {
        // Arrange
        var settings = new DbSettings()
        {
            General = new()
            {
                RunOnTorrentCompleteFileName = "/bin/echo"
            }
        };

        var mocks = new Mocks();

        mocks.TorrentDataMock.Setup(t => t.GetById(torrent.TorrentId)).Returns(Task.FromResult<Torrent?>(torrent));
        mocks.DownloadsMock.Setup(d => d.GetForTorrent(torrent.TorrentId)).ReturnsAsync(downloads);

        var downloadPath = $"{settings.DownloadClient.DownloadPath}/{torrent.Category}";
        var torrentPath = $"{downloadPath}/{torrent.RdName}";
        var filePath = $"{torrentPath}/{downloads[0].FileName}";

        var fileSystemMock = new MockFileSystem(new Dictionary<String, MockFileData>
        {
            {
                filePath, new("Test file")
            },
        });

        var torrents = new TorrentsService(mocks.TorrentsLoggerMock.Object,
                                           mocks.TorrentDataMock.Object,
                                           mocks.DownloadsMock.Object,
                                           mocks.ProcessFactoryMock.Object,
                                           fileSystemMock,
                                           null!, // Torrent Clients are not used by `RunTorrentComplete`, this is fine
                                           null!,
                                           null!,
                                           null!,
                                           null!);

        mocks.ProcessMock.Setup(p => p.WaitForExit(It.IsAny<Int32>()))
             .Callback(() =>
             {
                 mocks.ProcessMock.Raise(m => m.ErrorDataReceived += null, this, "error-line 1");
                 mocks.ProcessMock.Raise(m => m.ErrorDataReceived += null, this, "error-line 2");
                 mocks.ProcessMock.Raise(m => m.ErrorDataReceived += null, this, "error-line 3");
             })
             .Returns(true);

        // Act
        await torrents.RunTorrentComplete(torrent.TorrentId, settings);

        // Assert
        mocks.ProcessMock.Verify(p => p.BeginErrorReadLine(), Times.Once);

        var messages = mocks.TorrentsLoggerMock.Invocations.Where(i => i.Method.Name == "Log").Select(i => i.Arguments[2].ToString()).Where(m => m != null).ToList();
        var exitedWithOutputMessages = messages.Where(m => Regex.IsMatch(m!, "exited with errors")).ToList();
        Assert.NotNull(exitedWithOutputMessages);
        Assert.Single(exitedWithOutputMessages);
        var exitedWithOutputMessage = exitedWithOutputMessages.First();
        Assert.NotNull(exitedWithOutputMessage);
        Assert.Matches("error-line 1", exitedWithOutputMessage);
        Assert.Matches("error-line 2", exitedWithOutputMessage);
        Assert.Matches("error-line 3", exitedWithOutputMessage);
    }
}
